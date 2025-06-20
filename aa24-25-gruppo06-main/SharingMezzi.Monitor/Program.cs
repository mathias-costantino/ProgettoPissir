using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Monitor
{
    class Program
    {
        private static IManagedMqttClient? _mqttClient;
        private static bool _isRunning = true;
        private static bool _showDetailedPayload = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine(" SharingMezzi MQTT Monitor");
            Console.WriteLine("============================");
            
            await ConnectToMqttAsync();
            
            // Subscribe to all SharingMezzi topics
            await SubscribeToTopicsAsync();
            
            // Start command loop
            await RunCommandLoopAsync();
        }

        private static async Task ConnectToMqttAsync()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("SharingMezzi-Monitor")
                    .WithTcpServer("localhost", 1883)
                    .WithCleanSession()
                    .Build())
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
            _mqttClient.ConnectedAsync += OnConnected;
            _mqttClient.DisconnectedAsync += OnDisconnected;

            await _mqttClient.StartAsync(options);
            
            Console.WriteLine("Connessione al broker MQTT in corso...");
            await Task.Delay(2000); // Wait for connection
        }

        private static async Task SubscribeToTopicsAsync()
        {
            var topics = new[]
            {
                "parking/+/mezzi",
                "parking/+/sensori/#",
                "parking/+/sistema/#",
                "mezzi/+/comandi/#",
                "mezzi/+/stato/#",
                "sistema/#"
            };

            foreach (var topic in topics)
            {
                await _mqttClient!.SubscribeAsync(topic);
                Console.WriteLine($" Subscribed to: {topic}");
            }
        }

        private static Task OnConnected(MqttClientConnectedEventArgs args)
        {
            Console.WriteLine(" Connesso al broker MQTT");
            return Task.CompletedTask;
        }

        private static Task OnDisconnected(MqttClientDisconnectedEventArgs args)
        {
            Console.WriteLine(" Disconnesso dal broker MQTT");
            return Task.CompletedTask;
        }

        private static Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($" [{timestamp}] Topic: {topic}");
            Console.ForegroundColor = ConsoleColor.White;
            
            try
            {
                // Parse as SharingMezziMqttMessage and show only essential info
                var message = JsonSerializer.Deserialize<SharingMezziMqttMessage>(payload);
                if (message != null)
                {
                    if (_showDetailedPayload)
                    {
                        var jsonDoc = JsonDocument.Parse(payload);
                        var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($" Full Payload: {prettyJson}");
                    }
                    else
                    {
                        ShowEssentialInfo(message);
                    }
                }
                else
                {
                    Console.WriteLine($" Raw: {payload}");
                }
            }
            catch
            {
                // If not our message format, show raw payload
                Console.WriteLine($" Raw: {payload}");
            }
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("─────────────────────────────────────");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        private static void ShowEssentialInfo(SharingMezziMqttMessage message)
        {
            var info = new List<string>();
            
            // Always show message type and IDs
            info.Add($"Type: {message.MessageType}");
            if (message.MezzoId.HasValue) info.Add($"Mezzo: {message.MezzoId}");
            if (message.ParcheggioId.HasValue) info.Add($"Parking: {message.ParcheggioId}");
            if (message.CorsaId.HasValue) info.Add($"Corsa: {message.CorsaId}");
            
            // Show relevant data based on message type
            switch (message.MessageType)
            {
                case SharingMezziMessageType.BatteryUpdate:
                    if (message.BatteryLevel.HasValue) 
                        info.Add($" {message.BatteryLevel}%");
                    if (message.IsCharging.HasValue && message.IsCharging.Value)
                        info.Add(" Charging");
                    break;
                    
                case SharingMezziMessageType.LockStatusUpdate:
                case SharingMezziMessageType.UnlockCommand:
                case SharingMezziMessageType.LockCommand:
                    info.Add($"Lock State: {message.LockState}");
                    if (!string.IsNullOrEmpty(message.Command))
                        info.Add($"Cmd: {message.Command}");
                    break;
                    
                case SharingMezziMessageType.MovementUpdate:
                    if (message.IsMoving.HasValue)
                        info.Add($" Moving: {message.IsMoving}");
                    if (message.Speed.HasValue)
                        info.Add($"Speed: {message.Speed:F1} km/h");
                    break;
                    
                case SharingMezziMessageType.ErrorReport:
                    info.Add($" Error: {message.StatusMessage}");
                    break;
                    
                case SharingMezziMessageType.CommandAcknowledge:
                    info.Add($" ACK: {message.ExecutionStatus}");
                    if (!string.IsNullOrEmpty(message.StatusMessage))
                        info.Add($"Msg: {message.StatusMessage}");
                    break;
            }
            
            Console.WriteLine($" {string.Join(" | ", info)}");
        }

        private static async Task RunCommandLoopAsync()
        {
            Console.WriteLine();
            Console.WriteLine(" Comandi disponibili:");
            Console.WriteLine("  1. send - Invia messaggio custom");
            Console.WriteLine("  2. battery - Simula aggiornamento batteria");
            Console.WriteLine("  3. lock - Simula comando sblocco");
            Console.WriteLine("  4. error - Simula errore IoT");
            Console.WriteLine("  5. detail - Toggle payload dettagliato");
            Console.WriteLine("  6. help - Mostra comandi");
            Console.WriteLine("  7. quit - Esci");
            Console.WriteLine();

            while (_isRunning)
            {
                Console.Write(" Comando > ");
                var input = Console.ReadLine()?.ToLower().Trim();

                try
                {
                    switch (input)
                    {
                        case "1":
                        case "send":
                            await SendCustomMessageAsync();
                            break;
                        case "2":
                        case "battery":
                            await SendBatteryUpdateAsync();
                            break;
                        case "3":
                        case "lock":
                            await SendLockCommandAsync();
                            break;
                        case "4":
                        case "error":
                            await SendErrorReportAsync();
                            break;
                        case "5":
                        case "detail":
                            _showDetailedPayload = !_showDetailedPayload;
                            Console.WriteLine($" Payload dettagliato: {(_showDetailedPayload ? "ON" : "OFF")}");
                            break;
                        case "6":
                        case "help":
                            ShowHelp();
                            break;
                        case "7":
                        case "quit":
                        case "exit":
                            _isRunning = false;
                            break;
                        default:
                            if (!string.IsNullOrEmpty(input))
                                Console.WriteLine(" Comando non riconosciuto. Digita 'help' per vedere i comandi disponibili.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Errore: {ex.Message}");
                }
            }

            await _mqttClient!.StopAsync();
            Console.WriteLine(" Monitor MQTT chiuso.");
        }

        private static async Task SendCustomMessageAsync()
        {
            Console.Write(" Topic: ");
            var topic = Console.ReadLine();
            
            Console.Write(" Messaggio (JSON): ");
            var message = Console.ReadLine();

            if (!string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(message))
            {
                await PublishMessageAsync(topic, message);
                Console.WriteLine(" Messaggio inviato!");
            }
        }

        private static async Task SendBatteryUpdateAsync()
        {
            Console.Write(" ID Mezzo: ");
            if (int.TryParse(Console.ReadLine(), out int mezzoId))
            {
                Console.Write(" Livello batteria (0-100): ");
                if (int.TryParse(Console.ReadLine(), out int batteryLevel))
                {
                    var message = new SharingMezziMqttMessage
                    {
                        MessageType = SharingMezziMessageType.BatteryUpdate,
                        MezzoId = mezzoId,
                        ParcheggioId = 1, // Default parking
                        BatteryLevel = Math.Max(0, Math.Min(100, batteryLevel)),
                        Voltage = batteryLevel * 0.36f + 36,
                        Current = -2.5f,
                        Temperature = 25,
                        IsCharging = false,
                        Timestamp = DateTime.UtcNow
                    };

                    var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
                    await PublishMessageAsync("parking/1/sensori/batteria", json);
                    Console.WriteLine(" Aggiornamento batteria inviato!");
                }
            }
        }

        private static async Task SendLockCommandAsync()
        {
            Console.Write("ID Mezzo: ");
            if (int.TryParse(Console.ReadLine(), out int mezzoId))
            {
                Console.Write(" Comando (lock/unlock): ");
                var command = Console.ReadLine()?.ToLower();

                if (command == "lock" || command == "unlock")
                {
                    var messageType = command == "unlock" ? 
                        SharingMezziMessageType.UnlockCommand : 
                        SharingMezziMessageType.LockCommand;

                    var message = new SharingMezziMqttMessage
                    {
                        MessageType = messageType,
                        MezzoId = mezzoId,
                        ParcheggioId = 1,
                        Command = command,
                        CorsaId = command == "unlock" ? 999 : null,
                        Timestamp = DateTime.UtcNow
                    };

                    var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
                    await PublishMessageAsync($"mezzi/{mezzoId}/comandi/sblocco", json);
                    Console.WriteLine($" Comando {command} inviato!");
                }
            }
        }

        private static async Task SendErrorReportAsync()
        {
            Console.Write(" ID Mezzo: ");
            if (int.TryParse(Console.ReadLine(), out int mezzoId))
            {
                Console.Write(" Descrizione errore: ");
                var errorDesc = Console.ReadLine() ?? "Errore generico";

                var message = new SharingMezziMqttMessage
                {
                    MessageType = SharingMezziMessageType.ErrorReport,
                    MezzoId = mezzoId,
                    ParcheggioId = 1,
                    DeviceId = $"device_{mezzoId}",
                    StatusMessage = errorDesc,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
                await PublishMessageAsync("sistema/errori", json);
                Console.WriteLine(" Segnalazione errore inviata!");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine(" Comandi disponibili:");
            Console.WriteLine("  send     - Invia messaggio personalizzato (topic + JSON)");
            Console.WriteLine("  battery  - Simula aggiornamento batteria per un mezzo");
            Console.WriteLine("  lock     - Invia comando sblocco/blocco a un mezzo");
            Console.WriteLine("  error    - Simula segnalazione errore IoT");
            Console.WriteLine("  detail   - Attiva/disattiva payload completo");
            Console.WriteLine("  help     - Mostra questa guida");
            Console.WriteLine("  quit     - Chiudi il monitor");
            Console.WriteLine();
            Console.WriteLine(" Topic monitorati:");
            Console.WriteLine("  - parking/+/mezzi (aggiornamenti mezzi)");
            Console.WriteLine("  - parking/+/sensori/# (dati sensori)");
            Console.WriteLine("  - mezzi/+/comandi/# (comandi ai mezzi)");
            Console.WriteLine("  - sistema/# (messaggi di sistema)");
            Console.WriteLine();
            Console.WriteLine($" Modalità payload: {(_showDetailedPayload ? "DETTAGLIATO" : "ESSENZIALE")}");
            Console.WriteLine();
        }

        private static async Task PublishMessageAsync(string topic, string message)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient!.InternalClient.PublishAsync(mqttMessage);
        }
    }
}
