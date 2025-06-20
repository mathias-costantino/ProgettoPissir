using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using System.Text;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace SharingMezzi.IoT.Clients
{
    /// <summary>
    /// Client MQTT che simula un dispositivo IoT montato su un mezzo
    /// Gestisce sensori (batteria, GPS, lock) e attuatori (sblocco, allarme)
    /// </summary>
    public class MezzoIoTClient : IDisposable
    {
        private readonly IMqttClient _mqttClient;
        private readonly ILogger<MezzoIoTClient> _logger;
        private readonly Timer _heartbeatTimer = new(TimeSpan.FromMinutes(5)); // Heartbeat ogni 5 min
        private readonly Timer _batteryTimer = new(TimeSpan.FromSeconds(30));  // Batteria ogni 30 sec
        private readonly Timer _movementTimer = new(TimeSpan.FromSeconds(10)); // Movimento ogni 10 sec
        
        private MqttClientOptions? _clientOptions;
        private CancellationTokenSource _cancellationTokenSource = new();
        
        // Stato simulato del mezzo
        public MezzoSimulationState State { get; private set; }
        public bool IsConnected => _mqttClient.IsConnected;
        
        // Lock per thread safety
        private readonly object _stateLock = new();
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        
        public MezzoIoTClient(int mezzoId, ILogger<MezzoIoTClient> logger)
        {
            _logger = logger;
            State = new MezzoSimulationState(mezzoId);
            
            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            
            // Setup timers
            _heartbeatTimer.Elapsed += async (s, e) => await SendHeartbeatAsync();
            _batteryTimer.Elapsed += async (s, e) => await SendBatteryUpdateAsync();
            _movementTimer.Elapsed += async (s, e) => await SendMovementUpdateAsync();
        }

        /// <summary>
        /// Inizializza e connette il client IoT
        /// </summary>
        public async Task<bool> InitializeAsync(string brokerHost = "localhost", int brokerPort = 1883)
        {
            try
            {
                _clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId($"mezzo_{State.MezzoId}_{Guid.NewGuid():N}")
                    .WithTcpServer(brokerHost, brokerPort)
                    .WithKeepAlivePeriod(TimeSpan.FromMinutes(2))
                    .WithCleanSession()
                    .Build();

                var isConnected = await ConnectAsync();
                if (isConnected)
                {
                    await SubscribeToCommands();
                    
                    // Aspetta che la registrazione del dispositivo sia completata prima di avviare i timer
                    await Task.Delay(3000);
                    StartTimers();
                    
                    _logger.LogInformation("Mezzo {MezzoId} IoT client initialized and connected", State.MezzoId);
                }
                
                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Mezzo {MezzoId} IoT client", State.MezzoId);
                return false;
            }
        }

        /// <summary>
        /// Connette al broker MQTT
        /// </summary>
        private async Task<bool> ConnectAsync()
        {
            await _connectionSemaphore.WaitAsync();
            
            try
            {
                if (_mqttClient.IsConnected)
                    return true;

                await _mqttClient.ConnectAsync(_clientOptions, _cancellationTokenSource.Token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting Mezzo {MezzoId} to MQTT broker", State.MezzoId);
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Sottoscrive ai topic dei comandi per questo mezzo
        /// </summary>
        private async Task SubscribeToCommands()
        {
            var commandTopics = new[]
            {
                $"parking/{State.ParkingId}/stato_mezzi/{State.MezzoId}",
                $"parking/{State.ParkingId}/attuatori/sblocco/{State.MezzoId}",
                $"parking/{State.ParkingId}/attuatori/allarme/{State.MezzoId}",
                $"parking/{State.ParkingId}/comandi/mezzo/{State.MezzoId}/#"
            };

            foreach (var topic in commandTopics)
            {
                await _mqttClient.SubscribeAsync(topic);
                _logger.LogDebug("Subscribed to: {Topic}", topic);
            }
        }

        /// <summary>
        /// Avvia i timer per invio dati periodici
        /// </summary>
        private void StartTimers()
        {
            _heartbeatTimer.Start();
            _batteryTimer.Start();
            
            // Timer movimento solo se mezzo in movimento
            if (State.IsMoving)
                _movementTimer.Start();
        }

        /// <summary>
        /// Ferma tutti i timer
        /// </summary>
        private void StopTimers()
        {
            _heartbeatTimer.Stop();
            _batteryTimer.Stop();
            _movementTimer.Stop();
        }

        /// <summary>
        /// Gestisce connessione al broker
        /// </summary>
        private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            _logger.LogInformation("Mezzo {MezzoId} connected to MQTT broker", State.MezzoId);
            await SendDeviceRegistration();
            
            // Aspetta che la registrazione sia processata dal broker prima di iniziare i sensori
            await Task.Delay(2000);
        }

        /// <summary>
        /// Gestisce disconnessione dal broker
        /// </summary>
        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("Mezzo {MezzoId} disconnected: {Reason}", State.MezzoId, args.ReasonString);
            StopTimers();
            
            // Tenta riconnessione automatica
            _ = Task.Run(async () => await AttemptReconnection());
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gestisce messaggi ricevuti (comandi dal broker)
        /// </summary>
        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
                var topic = args.ApplicationMessage.Topic;
                
                _logger.LogDebug("Mezzo {MezzoId} received command on {Topic}: {Payload}", 
                    State.MezzoId, topic, payload);

                var message = JsonSerializer.Deserialize<SharingMezziMqttMessage>(payload);
                if (message != null)
                {
                    await ProcessCommand(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing received message for Mezzo {MezzoId}", State.MezzoId);
            }
        }

        /// <summary>
        /// Processa comandi ricevuti dal broker
        /// </summary>
        private async Task ProcessCommand(SharingMezziMqttMessage command)
        {
            var startTime = DateTime.Now;
            var acknowledgment = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.CommandAcknowledge,
                MessageId = command.MessageId,
                MezzoId = State.MezzoId,
                ParcheggioId = State.ParkingId,
                Command = command.Command,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                switch (command.MessageType)
                {
                    case SharingMezziMessageType.UnlockCommand:
                        await ExecuteUnlockCommand(command);
                        acknowledgment.ExecutionStatus = "success";
                        acknowledgment.StatusMessage = "Mezzo sbloccato con successo";
                        break;
                        
                    case SharingMezziMessageType.LockCommand:
                        await ExecuteLockCommand(command);
                        acknowledgment.ExecutionStatus = "success";
                        acknowledgment.StatusMessage = "Mezzo bloccato con successo";
                        break;
                        
                    case SharingMezziMessageType.AlarmCommand:
                        await ExecuteAlarmCommand(command);
                        acknowledgment.ExecutionStatus = "success";
                        acknowledgment.StatusMessage = "Allarme attivato";
                        break;
                        
                    case SharingMezziMessageType.ResetCommand:
                        await ExecuteResetCommand();
                        acknowledgment.ExecutionStatus = "success";
                        acknowledgment.StatusMessage = "Dispositivo resettato";
                        break;
                        
                    default:
                        acknowledgment.ExecutionStatus = "error";
                        acknowledgment.StatusMessage = $"Comando non riconosciuto: {command.MessageType}";
                        break;
                }
            }
            catch (Exception ex)
            {
                acknowledgment.ExecutionStatus = "error";
                acknowledgment.StatusMessage = ex.Message;
                _logger.LogError(ex, "Error executing command {Command} for Mezzo {MezzoId}", 
                    command.Command, State.MezzoId);
            }

            // Calcola tempo esecuzione e invia ACK
            acknowledgment.ExecutionTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
            await PublishMessage(acknowledgment, "feedback");
        }

        /// <summary>
        /// Esegue comando di sblocco mezzo
        /// </summary>
        private async Task ExecuteUnlockCommand(SharingMezziMqttMessage command)
        {
            // Simula tempo operazione meccanica
            await Task.Delay(Random.Shared.Next(1000, 3000));
            
            lock (_stateLock)
            {
                State.LockState = "unlocked";
                State.IsSecure = false;
                State.LastOperation = DateTime.UtcNow;
                State.CorsaId = command.CorsaId;
                
                // Se mezzo elettrico, inizia a scaricarsi
                if (State.IsElettrico && State.BatteryLevel > 10)
                {
                    State.IsMoving = true;
                    _movementTimer.Start();
                }
            }
            
            _logger.LogInformation("Mezzo {MezzoId} sbloccato per corsa {CorsaId}", 
                State.MezzoId, command.CorsaId);
            
            // Invia aggiornamento stato lock
            await SendLockStatusUpdate();
        }

        /// <summary>
        /// Esegue comando di blocco mezzo
        /// </summary>
        private async Task ExecuteLockCommand(SharingMezziMqttMessage command)
        {
            await Task.Delay(Random.Shared.Next(500, 2000));
            
            lock (_stateLock)
            {
                State.LockState = "locked";
                State.IsSecure = true;
                State.LastOperation = DateTime.UtcNow;
                State.IsMoving = false;
                State.CorsaId = null;
            }
            
            _movementTimer.Stop();
            
            _logger.LogInformation("Mezzo {MezzoId} bloccato", State.MezzoId);
            await SendLockStatusUpdate();
        }

        /// <summary>
        /// Esegue comando allarme
        /// </summary>
        private async Task ExecuteAlarmCommand(SharingMezziMqttMessage command)
        {
            await Task.Delay(500);
            
            lock (_stateLock)
            {
                State.AlarmActive = true;
            }
            
            _logger.LogWarning("Allarme attivato per Mezzo {MezzoId}", State.MezzoId);
            
            // Allarme automatico per 30 secondi
            _ = Task.Run(async () =>
            {
                await Task.Delay(30000);
                lock (_stateLock)
                {
                    State.AlarmActive = false;
                }
            });
        }

        /// <summary>
        /// Esegue reset dispositivo
        /// </summary>
        private async Task ExecuteResetCommand()
        {
            await Task.Delay(2000);
            
            lock (_stateLock)
            {
                State.Reset();
            }
            
            _logger.LogInformation("Dispositivo Mezzo {MezzoId} resettato", State.MezzoId);
        }

        /// <summary>
        /// Invia heartbeat dispositivo
        /// </summary>
        private async Task SendHeartbeatAsync()
        {
            var heartbeat = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.SensorHeartbeat,
                DeviceId = $"mezzo_{State.MezzoId}",
                MezzoId = State.MezzoId,
                ParcheggioId = State.ParkingId,
                IsOnline = true,
                SignalStrength = -45 + Random.Shared.Next(-10, 10), // Simula variazione segnale
                FirmwareVersion = "1.2.3",
                UptimeHours = (float)(DateTime.UtcNow - State.StartTime).TotalHours
            };

            await PublishMessage(heartbeat, "heartbeat");
        }

        /// <summary>
        /// Invia aggiornamento batteria
        /// </summary>
        private async Task SendBatteryUpdateAsync()
        {
            return;
            
            if (!State.IsElettrico)
                return;

            if (State.MezzoId <= 0)
            {
                _logger.LogError("Invalid MezzoId for battery update: {MezzoId}", State.MezzoId);
                return;
            }
            
            if (State.ParkingId <= 0)
            {
                _logger.LogError("Invalid ParkingId for battery update: {ParkingId}", State.ParkingId);
                return;
            }

            // Simula scarica batteria se in movimento
            lock (_stateLock)
            {
                if (State.IsMoving && State.BatteryLevel > 0)
                {
                    var drain = Random.Shared.Next(1, 3);
                    State.BatteryLevel = Math.Max(0, State.BatteryLevel - drain);
                }
                // Simula ricarica se fermo e livello basso
                else if (!State.IsMoving && State.BatteryLevel < 90)
                {
                    var charge = Random.Shared.Next(0, 2);
                    State.BatteryLevel = Math.Min(100, State.BatteryLevel + charge);
                }
                
                // Assicura che il livello batteria sia sempre valido
                State.BatteryLevel = Math.Max(0, Math.Min(100, State.BatteryLevel));
            }

            var batteryUpdate = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.BatteryUpdate,
                MezzoId = State.MezzoId,
                ParcheggioId = State.ParkingId,
                BatteryLevel = State.BatteryLevel,
                Voltage = State.BatteryLevel * 0.36f + 36, // Simula voltage
                Current = State.IsMoving ? -2.5f : 0.1f,   // Negativo = scarica
                Temperature = 20 + Random.Shared.Next(-5, 15),
                IsCharging = !State.IsMoving && State.BatteryLevel < 90
            };

            // Validazione finale messaggio (doppio controllo per sicurezza)
            if (!batteryUpdate.MezzoId.HasValue || batteryUpdate.MezzoId.Value <= 0)
            {
                _logger.LogError("Final validation failed - Invalid MezzoId: {MezzoId}", batteryUpdate.MezzoId);
                return;
            }
            
            if (!batteryUpdate.ParcheggioId.HasValue || batteryUpdate.ParcheggioId.Value <= 0)
            {
                _logger.LogError("Final validation failed - Invalid ParcheggioId: {ParcheggioId}", batteryUpdate.ParcheggioId);
                return;
            }
            
            if (!batteryUpdate.BatteryLevel.HasValue)
            {
                _logger.LogError("Final validation failed - Null BatteryLevel for Mezzo {MezzoId}", State.MezzoId);
                return;
            }

            await PublishMessage(batteryUpdate, "sensori/batteria");
        }

        /// <summary>
        /// Invia aggiornamento movimento
        /// </summary>
        private async Task SendMovementUpdateAsync()
        {
            if (!State.IsMoving)
                return;

            // Simula movimento casuale
            lock (_stateLock)
            {
                State.Latitude += (Random.Shared.NextDouble() - 0.5) * 0.001;
                State.Longitude += (Random.Shared.NextDouble() - 0.5) * 0.001;
                State.Speed = Random.Shared.Next(5, 25);
            }

            var movementUpdate = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.MovementUpdate,
                MezzoId = State.MezzoId,
                ParcheggioId = State.ParkingId,
                Latitude = State.Latitude,
                Longitude = State.Longitude,
                Speed = State.Speed,
                IsMoving = State.IsMoving,
                Acceleration = Random.Shared.Next(-2, 5)
            };

            await PublishMessage(movementUpdate, "sensori/movimento");
        }

        /// <summary>
        /// Invia aggiornamento stato lock
        /// </summary>
        private async Task SendLockStatusUpdate()
        {
            var lockUpdate = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.LockStatusUpdate,
                MezzoId = State.MezzoId,
                ParcheggioId = State.ParkingId,
                LockState = State.LockState,
                IsSecure = State.IsSecure,
                LastOperation = State.LastOperation
            };

            await PublishMessage(lockUpdate, "sensori/sblocco");
        }

        /// <summary>
        /// Invia registrazione dispositivo
        /// </summary>
        private async Task SendDeviceRegistration()
        {
            // Validazione prerequisiti rigorosa
            if (State.MezzoId <= 0)
            {
                _logger.LogError("Cannot send device registration - Invalid MezzoId: {MezzoId}", State.MezzoId);
                return;
            }
            
            if (State.ParkingId <= 0)
            {
                _logger.LogError("Cannot send device registration - Invalid ParkingId: {ParkingId}", State.ParkingId);
                return;
            }

            var registration = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.DeviceRegistration,
                DeviceId = $"mezzo_{State.MezzoId}",
                MezzoId = State.MezzoId,
                ParcheggioId = State.ParkingId,
                FirmwareVersion = "1.2.3",
                IsOnline = true
            };

            // Validazione finale messaggio di registrazione
            if (!registration.MezzoId.HasValue || registration.MezzoId.Value <= 0)
            {
                _logger.LogError("Final validation failed for device registration - Invalid MezzoId: {MezzoId}", registration.MezzoId);
                return;
            }
            
            if (!registration.ParcheggioId.HasValue || registration.ParcheggioId.Value <= 0)
            {
                _logger.LogError("Final validation failed for device registration - Invalid ParcheggioId: {ParcheggioId}", registration.ParcheggioId);
                return;
            }
            
            if (string.IsNullOrEmpty(registration.DeviceId))
            {
                _logger.LogError("Final validation failed for device registration - Empty DeviceId for Mezzo {MezzoId}", State.MezzoId);
                return;
            }

            await PublishMessage(registration, "dispositivi");
            _logger.LogDebug("Device registration sent - Mezzo {MezzoId}, DeviceId: {DeviceId}, ParkingId: {ParkingId}", 
                State.MezzoId, registration.DeviceId, State.ParkingId);
        }

        /// <summary>
        /// Pubblica messaggio MQTT
        /// </summary>
        private async Task PublishMessage(SharingMezziMqttMessage message, string subtopic)
        {
            try
            {
                var topic = $"parking/{State.ParkingId}/{subtopic}/{State.MezzoId}";
                var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(payload))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(mqttMessage);
                
                _logger.LogDebug("Published to {Topic}: {MessageType}", topic, message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message for Mezzo {MezzoId}", State.MezzoId);
            }
        }

        /// <summary>
        /// Tenta riconnessione automatica
        /// </summary>
        private async Task AttemptReconnection()
        {
            var attempt = 0;
            const int maxAttempts = 10;
            
            while (!_mqttClient.IsConnected && attempt < maxAttempts && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                attempt++;
                
                try
                {
                    _logger.LogInformation("Attempting reconnection {Attempt}/{MaxAttempts} for Mezzo {MezzoId}", 
                        attempt, maxAttempts, State.MezzoId);
                        
                    await Task.Delay(5000 * attempt, _cancellationTokenSource.Token);
                    
                    if (await ConnectAsync())
                    {
                        await SubscribeToCommands();
                        
                        // Aspetta che la registrazione del dispositivo sia completata
                        await Task.Delay(3000, _cancellationTokenSource.Token);
                        StartTimers();
                        _logger.LogInformation("Mezzo {MezzoId} reconnected successfully", State.MezzoId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reconnection attempt {Attempt} failed for Mezzo {MezzoId}", 
                        attempt, State.MezzoId);
                }
            }
            
            _logger.LogError("Failed to reconnect Mezzo {MezzoId} after {MaxAttempts} attempts", 
                State.MezzoId, maxAttempts);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            StopTimers();
            
            _heartbeatTimer?.Dispose();
            _batteryTimer?.Dispose();
            _movementTimer?.Dispose();
            
            if (_mqttClient.IsConnected)
            {
                _mqttClient.DisconnectAsync().Wait(5000);
            }
            
            _mqttClient?.Dispose();
            _cancellationTokenSource?.Dispose();
            _connectionSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// Stato simulato di un mezzo IoT
    /// </summary>
    public class MezzoSimulationState
    {
        public int MezzoId { get; }
        public int ParkingId { get; set; } = 1; // Default parking
        public bool IsElettrico { get; set; } = true;
        public DateTime StartTime { get; } = DateTime.UtcNow;
        
        // Stato batteria
        public int BatteryLevel { get; set; } = 85;
        
        // Stato meccanismo
        public string LockState { get; set; } = "locked";
        public bool IsSecure { get; set; } = true;
        public DateTime? LastOperation { get; set; }
        public int? CorsaId { get; set; }
        
        // Stato movimento
        public bool IsMoving { get; set; } = false;
        public double Latitude { get; set; } = 45.0702 + (Random.Shared.NextDouble() - 0.5) * 0.01; // Torino area
        public double Longitude { get; set; } = 7.6869 + (Random.Shared.NextDouble() - 0.5) * 0.01;
        public float Speed { get; set; } = 0;
        
        // Altri stati
        public bool AlarmActive { get; set; } = false;
        
        public MezzoSimulationState(int mezzoId)
        {
            MezzoId = mezzoId;
        }
        
        public void Reset()
        {
            LockState = "locked";
            IsSecure = true;
            IsMoving = false;
            Speed = 0;
            AlarmActive = false;
            CorsaId = null;
        }
    }
}