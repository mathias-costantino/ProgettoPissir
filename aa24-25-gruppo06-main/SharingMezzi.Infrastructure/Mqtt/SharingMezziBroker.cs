using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Interfaces.Services;
using SharingMezzi.Core.Entities;
using SharingMezzi.Infrastructure.Services;
using System.Text;
using System.Text.Json;

public class SharingMezziBroker : BackgroundService, IDisposable
{
    private readonly MqttServer _mqttServer;
    private readonly MqttServerOptions _options;
    private readonly ILogger<SharingMezziBroker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SharingMezziBroker(
        ILogger<SharingMezziBroker> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        _options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1883)
            .WithKeepAlive()
            .Build();

        _mqttServer = new MqttFactory().CreateMqttServer(_options);
        
        _mqttServer.ClientConnectedAsync += OnClientConnectedAsync;
        _mqttServer.ClientDisconnectedAsync += OnClientDisconnectedAsync;
        _mqttServer.InterceptingPublishAsync += OnMessageReceivedAsync;
    }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting SharingMezzi MQTT Broker...");
            
            try
            {
                await _mqttServer.StartAsync();
                _logger.LogInformation("SharingMezzi MQTT Broker started on port 1883");
                
                // Mantieni il servizio attivo
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SharingMezzi MQTT Broker");
            }
            finally
            {
                await _mqttServer.StopAsync();
                _logger.LogInformation("SharingMezzi MQTT Broker stopped");
            }
        }

        private Task OnClientConnectedAsync(ClientConnectedEventArgs args)
        {
            _logger.LogInformation("IoT Device connected: {ClientId}", args.ClientId);
            return Task.CompletedTask;
        }

        private Task OnClientDisconnectedAsync(ClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("IoT Device disconnected: {ClientId} - {Reason}", 
                args.ClientId, args.ReasonString);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gestisce tutti i messaggi MQTT ricevuti dai dispositivi IoT
        /// </summary>
        private async Task OnMessageReceivedAsync(InterceptingPublishEventArgs args)
        {
            if (args.ClientId == "SharingMezziBroker")
                return;

            try
            {
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
                var topic = args.ApplicationMessage.Topic;
                
                _logger.LogDebug("Received from {ClientId} on {Topic}: {Payload}", 
                    args.ClientId, topic, payload);

                var message = JsonSerializer.Deserialize<SharingMezziMqttMessage>(payload);
                if (message == null)
                {
                    _logger.LogWarning("Invalid MQTT message format from {ClientId}", args.ClientId);
                    return;
                }

                message.SourceTopic = topic;
                await ProcessIoTMessage(message, args.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message from {ClientId}", args.ClientId);
            }
        }

        /// <summary>
        /// Processa messaggio IoT e esegue azioni appropriate
        /// </summary>
        private async Task ProcessIoTMessage(SharingMezziMqttMessage message, string clientId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            
            try
            {
                switch (message.MessageType)
                {
                    case SharingMezziMessageType.BatteryUpdate:
                        await HandleBatteryUpdate(message, scope);
                        break;
                        
                    case SharingMezziMessageType.LockStatusUpdate:
                        await HandleLockStatusUpdate(message, scope);
                        break;
                        
                    case SharingMezziMessageType.MovementUpdate:
                        await HandleMovementUpdate(message, scope);
                        break;
                        
                    case SharingMezziMessageType.SlotOccupancyUpdate:
                        await HandleSlotOccupancyUpdate(message, scope);
                        break;
                        
                    case SharingMezziMessageType.CommandAcknowledge:
                        await HandleCommandAcknowledge(message, scope);
                        break;
                        
                    case SharingMezziMessageType.ErrorReport:
                        await HandleErrorReport(message, scope);
                        break;
                        
                    case SharingMezziMessageType.SensorHeartbeat:
                        await HandleSensorHeartbeat(message, scope);
                        break;
                        
                    default:
                        _logger.LogWarning("Unhandled message type: {MessageType} from {ClientId}", 
                            message.MessageType, clientId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message type {MessageType}", message.MessageType);
            }
        }

        /// <summary>
        /// Gestisce aggiornamenti batteria mezzi elettrici
        /// </summary>
        private async Task HandleBatteryUpdate(SharingMezziMqttMessage message, IServiceScope scope)
        {
            if (!message.MezzoId.HasValue || !message.BatteryLevel.HasValue)
            {
                _logger.LogDebug("Battery update ignored: MezzoId={MezzoId}, BatteryLevel={BatteryLevel}", 
                    message.MezzoId, message.BatteryLevel);
                return;
            }

            var mezzoRepo = scope.ServiceProvider.GetRequiredService<IMezzoRepository>();
            
            // Ottieni lo stato del mezzo prima dell'aggiornamento
            var mezzoPreAggiornamento = await mezzoRepo.GetByIdAsync(message.MezzoId.Value);
            var statoOriginale = mezzoPreAggiornamento?.Stato;
            
            // Aggiorna livello batteria nel database
            await mezzoRepo.UpdateBatteryLevelAsync(message.MezzoId.Value, message.BatteryLevel.Value);
            
            _logger.LogInformation("Battery update - Mezzo {MezzoId}: {BatteryLevel}%", 
                message.MezzoId, message.BatteryLevel);

            // Se batteria critica, imposta mezzo in manutenzione
            if (message.BatteryLevel < 5)
            {
                await mezzoRepo.UpdateStatusAsync(message.MezzoId.Value, StatoMezzo.Manutenzione);
            }
            
            // Verifica se lo stato è cambiato e aggiorna il parcheggio
            var mezzoPostAggiornamento = await mezzoRepo.GetByIdAsync(message.MezzoId.Value);
            if (mezzoPostAggiornamento != null && 
                mezzoPostAggiornamento.ParcheggioId.HasValue &&
                statoOriginale != mezzoPostAggiornamento.Stato)
            {
                var parcheggioService = scope.ServiceProvider.GetRequiredService<IParcheggioService>();
                await parcheggioService.UpdatePostiLiberiAsync(mezzoPostAggiornamento.ParcheggioId.Value);
                _logger.LogInformation("Updated parking {ParcheggioId} counts after MQTT battery-triggered state change for mezzo {MezzoId}", 
                    mezzoPostAggiornamento.ParcheggioId.Value, message.MezzoId.Value);
            }
        }

        /// <summary>
        /// Gestisce stato meccanismo sblocco/blocco
        /// </summary>
        private async Task HandleLockStatusUpdate(SharingMezziMqttMessage message, IServiceScope scope)
        {
            if (!message.MezzoId.HasValue)
                return;

            _logger.LogInformation("Lock status - Mezzo {MezzoId}: {LockState}", 
                message.MezzoId, message.LockState);

            // Se errore meccanismo, segnala
            if (message.LockState == "error" || message.LockState == "jammed")
            {
                await PublishMaintenanceAlert(message.MezzoId.Value, "Errore meccanismo sblocco");
            }
        }

        /// <summary>
        /// Gestisce dati movimento/posizione mezzi
        /// </summary>
        private async Task HandleMovementUpdate(SharingMezziMqttMessage message, IServiceScope scope)
        {
            if (!message.MezzoId.HasValue)
                return;

            _logger.LogDebug("Movement - Mezzo {MezzoId}: Speed {Speed} km/h, Moving: {IsMoving}", 
                message.MezzoId, message.Speed, message.IsMoving);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gestisce occupazione slot parcheggio
        /// </summary>
        private async Task HandleSlotOccupancyUpdate(SharingMezziMqttMessage message, IServiceScope scope)
        {
            if (!message.SlotId.HasValue || !message.ParcheggioId.HasValue)
                return;

            _logger.LogInformation("🅿️ Slot {SlotId} - Occupied: {IsOccupied}, Mezzo: {MezzoId}", 
                message.SlotId, message.IsSlotOccupied, message.MezzoId);

            // Aggiorna LED slot basato su occupazione
            var ledColor = message.IsSlotOccupied == true ? "red" : "green";
            await SendLedCommand(message.ParcheggioId.Value, message.SlotId.Value, ledColor);
        }

        /// <summary>
        /// Gestisce conferme esecuzione comandi
        /// </summary>
        private async Task HandleCommandAcknowledge(SharingMezziMqttMessage message, IServiceScope scope)
        {
            _logger.LogInformation("Command ACK - {Command} for Mezzo {MezzoId}: {Status} ({ExecutionTime}ms)", 
                message.Command, message.MezzoId, message.ExecutionStatus, message.ExecutionTime);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gestisce segnalazioni errori dai dispositivi
        /// </summary>
        private async Task HandleErrorReport(SharingMezziMqttMessage message, IServiceScope scope)
        {
            _logger.LogError("IoT Error Report - Device {DeviceId}, Mezzo {MezzoId}: {StatusMessage}", 
                message.DeviceId, message.MezzoId, message.StatusMessage);

            // Se errore critico, imposta mezzo in manutenzione
            if (message.MezzoId.HasValue)
            {
                var mezzoRepo = scope.ServiceProvider.GetRequiredService<IMezzoRepository>();
                await mezzoRepo.UpdateStatusAsync(message.MezzoId.Value, StatoMezzo.Guasto);
                
                // Aggiorna contatori parcheggio
                var mezzo = await mezzoRepo.GetByIdAsync(message.MezzoId.Value);
                if (mezzo != null && mezzo.ParcheggioId.HasValue)
                {
                    var parcheggioService = scope.ServiceProvider.GetRequiredService<IParcheggioService>();
                    await parcheggioService.UpdatePostiLiberiAsync(mezzo.ParcheggioId.Value);
                    _logger.LogInformation("Updated parking {ParcheggioId} counts after MQTT error-triggered state change for mezzo {MezzoId}", 
                        mezzo.ParcheggioId.Value, message.MezzoId.Value);
                }
            }
        }

        /// <summary>
        /// Gestisce heartbeat dispositivi IoT
        /// </summary>
        private async Task HandleSensorHeartbeat(SharingMezziMqttMessage message, IServiceScope scope)
        {
            _logger.LogDebug("Heartbeat - Device {DeviceId}: Online {IsOnline}, Signal {SignalStrength}dBm", 
                message.DeviceId, message.IsOnline, message.SignalStrength);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Invia comando sblocco mezzo
        /// </summary>
        public async Task SendUnlockCommand(int mezzoId, int? corsaId = null)
        {
            var message = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.UnlockCommand,
                MezzoId = mezzoId,
                CorsaId = corsaId,
                Command = "unlock",
                Priority = "high",
                TimeoutSeconds = 15
            };

            var topic = $"parking/1/stato_mezzi/{mezzoId}"; // Assumo parking 1
            await PublishToDevice(topic, message);
            
            _logger.LogInformation("Sent unlock command to Mezzo {MezzoId}", mezzoId);
        }

        /// <summary>
        /// Invia comando blocco mezzo
        /// </summary>
        public async Task SendLockCommand(int mezzoId, int? corsaId = null)
        {
            var message = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.LockCommand,
                MezzoId = mezzoId,
                CorsaId = corsaId,
                Command = "lock",
                Priority = "normal",
                TimeoutSeconds = 10
            };

            var topic = $"parking/1/stato_mezzi/{mezzoId}";
            await PublishToDevice(topic, message);
            
            _logger.LogInformation("Sent lock command to Mezzo {MezzoId}", mezzoId);
        }

        /// <summary>
        /// Invia comando controllo LED slot
        /// </summary>
        public async Task SendLedCommand(int parkingId, int slotId, string color, string pattern = "solid")
        {
            var message = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.LedCommand,
                SlotId = slotId,
                ParcheggioId = parkingId,
                LedColor = color,
                LedPattern = pattern,
                Command = "led_control"
            };

            var topic = $"parking/{parkingId}/attuatori/led/{slotId}";
            await PublishToDevice(topic, message);
            
            _logger.LogDebug("Sent LED command to Slot {SlotId}: {Color}", slotId, color);
        }

        /// <summary>
        /// Richiede stato batteria da mezzo elettrico tramite MQTT
        /// </summary>
        public async Task<int?> RequestBatteryStatus(int mezzoId, int timeoutSeconds = 10)
        {
            try
            {
                var requestId = Guid.NewGuid().ToString();
                var message = new SharingMezziMqttMessage
                {
                    MessageType = SharingMezziMessageType.RequestDeviceStatus,
                    MezzoId = mezzoId,
                    Command = "battery_check",
                    Priority = "high",
                    TimeoutSeconds = timeoutSeconds,
                    MessageId = requestId
                };

                var topic = $"parking/1/stato_mezzi/{mezzoId}"; // Assumo parking 1
                await PublishToDevice(topic, message);
                
                _logger.LogInformation("Sent battery check request to Mezzo {MezzoId} (Request: {RequestId})", 
                    mezzoId, requestId);

                // Simula attesa risposta con timeout
                await Task.Delay(200); // Simula latenza rete ridotta
                
                // Verifica se c'è un emulatore batteria disponibile
                using var scope = _serviceScopeFactory.CreateScope();
                var batteryEmulator = scope.ServiceProvider.GetService<BatteryEmulatorService>();
                
                if (batteryEmulator != null)
                {
                    var batteryStatus = batteryEmulator.GetBatteryStatus(mezzoId);
                    if (batteryStatus != null)
                    {
                        _logger.LogDebug("Battery response from emulator for Mezzo {MezzoId}: {Level}%", 
                            mezzoId, batteryStatus.BatteryLevel);
                        return batteryStatus.BatteryLevel;
                    }
                }
                
                // Fallback: simula risposta batteria casuale
                var simulatedBatteryLevel = new Random().Next(10, 100);
                _logger.LogDebug("Simulated battery response for Mezzo {MezzoId}: {Level}%", 
                    mezzoId, simulatedBatteryLevel);
                
                return simulatedBatteryLevel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting battery status for Mezzo {MezzoId}", mezzoId);
                return null;
            }
        }

        /// <summary>
        /// Pubblica messaggio a dispositivo IoT
        /// </summary>
        private async Task PublishToDevice(string topic, SharingMezziMqttMessage message)
        {
            try
            {
                var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(payload))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(topic.Contains("stato") || topic.Contains("command"))
                    .Build();

                await _mqttServer.InjectApplicationMessage(
                    new InjectedMqttApplicationMessage(mqttMessage) 
                    { 
                        SenderClientId = "SharingMezziBroker" 
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to topic {Topic}", topic);
            }
        }

        /// <summary>
        /// Pubblica alert manutenzione
        /// </summary>
        private async Task PublishMaintenanceAlert(int mezzoId, string reason)
        {
            var alertMessage = new SharingMezziMqttMessage
            {
                MessageType = SharingMezziMessageType.ErrorReport,
                MezzoId = mezzoId,
                StatusMessage = $"Manutenzione richiesta: {reason}",
                Priority = "high"
            };

            await PublishToDevice("mobishare/sistema/manutenzione", alertMessage);
        }

        public override void Dispose()
        {
            _mqttServer?.Dispose();
            base.Dispose();
        }
    }
