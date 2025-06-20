using Microsoft.AspNetCore.SignalR;
using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Api.Hubs
{
    /// <summary>
    /// Hub SignalR per eventi real-time IoT (sensori e attuatori)
    /// </summary>
    public class IoTHub : Hub
    {
        private readonly ILogger<IoTHub> _logger;

        public IoTHub(ILogger<IoTHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client si iscrive agli eventi IoT di un parcheggio specifico
        /// </summary>
        public async Task SubscribeToParcheggioIoT(int parcheggioId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"iot_parcheggio_{parcheggioId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to IoT events for parcheggio {ParcheggioId}", 
                Context.ConnectionId, parcheggioId);
        }

        /// <summary>
        /// Client si iscrive agli eventi IoT di un mezzo specifico
        /// </summary>
        public async Task SubscribeToMezzoIoT(int mezzoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"iot_mezzo_{mezzoId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to IoT events for mezzo {MezzoId}", 
                Context.ConnectionId, mezzoId);
        }

        /// <summary>
        /// Client si iscrive a tutti gli eventi IoT del sistema
        /// </summary>
        public async Task SubscribeToAllIoTEvents()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_iot_events");
            _logger.LogInformation("Client {ConnectionId} subscribed to all IoT events", Context.ConnectionId);
        }

        /// <summary>
        /// Client si iscrive agli allarmi e alert IoT
        /// </summary>
        public async Task SubscribeToIoTAlerts()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "iot_alerts");
            _logger.LogInformation("Client {ConnectionId} subscribed to IoT alerts", Context.ConnectionId);
        }

        /// <summary>
        /// Client si iscrive ai sensori di un tipo specifico
        /// </summary>
        public async Task SubscribeToSensorType(string sensorType)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sensor_{sensorType}");
            _logger.LogInformation("Client {ConnectionId} subscribed to {SensorType} sensors", 
                Context.ConnectionId, sensorType);
        }

        /// <summary>
        /// Client amministratore si iscrive alla diagnostica IoT
        /// </summary>
        public async Task SubscribeToIoTDiagnostics()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "iot_diagnostics");
            _logger.LogInformation("Client {ConnectionId} subscribed to IoT diagnostics", Context.ConnectionId);
        }

        /// <summary>
        /// Gestisce disconnessione client
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from IoTHub", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gestisce connessione client
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to IoTHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }

    /// <summary>
    /// Servizio per inviare notifiche IoT tramite SignalR
    /// </summary>
    public interface IIoTNotificationService
    {
        Task NotifySensorData(int parcheggioId, int? mezzoId, string sensorType, object data);
        Task NotifyActuatorCommand(int parcheggioId, int? mezzoId, string actuatorType, object command);
        Task NotifyDeviceHeartbeat(int parcheggioId, string deviceId, bool isOnline);
        Task NotifyBatteryAlert(int mezzoId, int batteryLevel);
        Task NotifyLockStatusChanged(int mezzoId, bool isLocked);
        Task NotifySlotOccupancyChanged(int parcheggioId, int slotId, bool isOccupied, double? weight);
        Task NotifyMovementDetected(int mezzoId, double latitude, double longitude, double speed);
        Task NotifyTemperatureAlert(int mezzoId, double temperature);
        Task NotifyVibrationAlert(int mezzoId, double intensity);
        Task NotifyDeviceOffline(int parcheggioId, string deviceId);
        Task NotifyCommandExecuted(int parcheggioId, int? mezzoId, string command, bool success);
        Task NotifySystemDiagnostics(Dictionary<string, object> diagnostics);
    }

    public class IoTNotificationService : IIoTNotificationService
    {
        private readonly IHubContext<IoTHub> _hubContext;
        private readonly ILogger<IoTNotificationService> _logger;

        public IoTNotificationService(IHubContext<IoTHub> hubContext, ILogger<IoTNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifySensorData(int parcheggioId, int? mezzoId, string sensorType, object data)
        {
            var groups = new List<string> { $"iot_parcheggio_{parcheggioId}", $"sensor_{sensorType}", "all_iot_events" };
            if (mezzoId.HasValue)
                groups.Add($"iot_mezzo_{mezzoId.Value}");

            await _hubContext.Clients.Groups(groups)
                .SendAsync("SensorData", new { 
                    ParcheggioId = parcheggioId, 
                    MezzoId = mezzoId, 
                    SensorType = sensorType, 
                    Data = data,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Notified {SensorType} sensor data for parcheggio {ParcheggioId}, mezzo {MezzoId}", 
                sensorType, parcheggioId, mezzoId);
        }

        public async Task NotifyActuatorCommand(int parcheggioId, int? mezzoId, string actuatorType, object command)
        {
            var groups = new List<string> { $"iot_parcheggio_{parcheggioId}", "all_iot_events" };
            if (mezzoId.HasValue)
                groups.Add($"iot_mezzo_{mezzoId.Value}");

            await _hubContext.Clients.Groups(groups)
                .SendAsync("ActuatorCommand", new { 
                    ParcheggioId = parcheggioId, 
                    MezzoId = mezzoId, 
                    ActuatorType = actuatorType, 
                    Command = command,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Notified {ActuatorType} command for parcheggio {ParcheggioId}, mezzo {MezzoId}", 
                actuatorType, parcheggioId, mezzoId);
        }

        public async Task NotifyDeviceHeartbeat(int parcheggioId, string deviceId, bool isOnline)
        {
            await _hubContext.Clients.Groups($"iot_parcheggio_{parcheggioId}", "iot_diagnostics")
                .SendAsync("DeviceHeartbeat", new { 
                    ParcheggioId = parcheggioId, 
                    DeviceId = deviceId, 
                    IsOnline = isOnline,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Notified heartbeat for device {DeviceId}: {Status}", deviceId, isOnline ? "ONLINE" : "OFFLINE");
        }

        public async Task NotifyBatteryAlert(int mezzoId, int batteryLevel)
        {
            await _hubContext.Clients.Groups($"iot_mezzo_{mezzoId}", "iot_alerts", "sensor_battery")
                .SendAsync("BatteryAlert", new { 
                    MezzoId = mezzoId, 
                    BatteryLevel = batteryLevel,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogWarning("Notified battery alert for mezzo {MezzoId}: {BatteryLevel}%", mezzoId, batteryLevel);
        }

        public async Task NotifyLockStatusChanged(int mezzoId, bool isLocked)
        {
            await _hubContext.Clients.Groups($"iot_mezzo_{mezzoId}", "all_iot_events")
                .SendAsync("LockStatusChanged", new { 
                    MezzoId = mezzoId, 
                    IsLocked = isLocked,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Notified lock status change for mezzo {MezzoId}: {Status}", 
                mezzoId, isLocked ? "LOCKED" : "UNLOCKED");
        }

        public async Task NotifySlotOccupancyChanged(int parcheggioId, int slotId, bool isOccupied, double? weight)
        {
            await _hubContext.Clients.Groups($"iot_parcheggio_{parcheggioId}", "sensor_slot", "all_iot_events")
                .SendAsync("SlotOccupancyChanged", new { 
                    ParcheggioId = parcheggioId, 
                    SlotId = slotId, 
                    IsOccupied = isOccupied,
                    Weight = weight,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Notified slot {SlotId} occupancy change: {Status} (weight: {Weight}kg)", 
                slotId, isOccupied ? "OCCUPIED" : "FREE", weight);
        }

        public async Task NotifyMovementDetected(int mezzoId, double latitude, double longitude, double speed)
        {
            await _hubContext.Clients.Groups($"iot_mezzo_{mezzoId}", "sensor_movement", "all_iot_events")
                .SendAsync("MovementDetected", new { 
                    MezzoId = mezzoId, 
                    Latitude = latitude, 
                    Longitude = longitude,
                    Speed = speed,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Notified movement for mezzo {MezzoId}: {Lat}, {Lon} at {Speed} km/h", 
                mezzoId, latitude, longitude, speed);
        }

        public async Task NotifyTemperatureAlert(int mezzoId, double temperature)
        {
            await _hubContext.Clients.Groups($"iot_mezzo_{mezzoId}", "iot_alerts", "sensor_temperature")
                .SendAsync("TemperatureAlert", new { 
                    MezzoId = mezzoId, 
                    Temperature = temperature,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogWarning("Notified temperature alert for mezzo {MezzoId}: {Temperature}°C", mezzoId, temperature);
        }

        public async Task NotifyVibrationAlert(int mezzoId, double intensity)
        {
            await _hubContext.Clients.Groups($"iot_mezzo_{mezzoId}", "iot_alerts", "sensor_vibration")
                .SendAsync("VibrationAlert", new { 
                    MezzoId = mezzoId, 
                    Intensity = intensity,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogWarning("Notified vibration alert for mezzo {MezzoId}: intensity {Intensity}", mezzoId, intensity);
        }

        public async Task NotifyDeviceOffline(int parcheggioId, string deviceId)
        {
            await _hubContext.Clients.Groups($"iot_parcheggio_{parcheggioId}", "iot_alerts", "iot_diagnostics")
                .SendAsync("DeviceOffline", new { 
                    ParcheggioId = parcheggioId, 
                    DeviceId = deviceId,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogError("Notified device {DeviceId} is OFFLINE in parcheggio {ParcheggioId}", deviceId, parcheggioId);
        }

        public async Task NotifyCommandExecuted(int parcheggioId, int? mezzoId, string command, bool success)
        {
            var groups = new List<string> { $"iot_parcheggio_{parcheggioId}", "all_iot_events" };
            if (mezzoId.HasValue)
                groups.Add($"iot_mezzo_{mezzoId.Value}");

            await _hubContext.Clients.Groups(groups)
                .SendAsync("CommandExecuted", new { 
                    ParcheggioId = parcheggioId, 
                    MezzoId = mezzoId, 
                    Command = command,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Notified command {Command} execution: {Result}", command, success ? "SUCCESS" : "FAILED");
        }

        public async Task NotifySystemDiagnostics(Dictionary<string, object> diagnostics)
        {
            await _hubContext.Clients.Group("iot_diagnostics")
                .SendAsync("SystemDiagnostics", new { 
                    Diagnostics = diagnostics,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Notified system diagnostics update");
        }
    }
}