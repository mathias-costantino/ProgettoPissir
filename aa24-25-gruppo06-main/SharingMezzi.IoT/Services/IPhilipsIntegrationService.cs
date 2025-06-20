using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;
using SharingMezzi.Infrastructure.Mqtt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace SharingMezzi.IoT.Services
{
    /// <summary>
    /// Servizio che integra gli eventi IoT/MQTT con l'emulatore Philips Hue
    /// per visualizzare graficamente lo stato di mezzi, slot e sistemi
    /// </summary>
    public class IoTPhilipsIntegrationService : BackgroundService
    {
        private readonly IMqttService _mqttService;
        private readonly IPhilipsHueService _hueService;
        private readonly ILogger<IoTPhilipsIntegrationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public IoTPhilipsIntegrationService(
            IMqttService mqttService,
            IPhilipsHueService hueService,
            ILogger<IoTPhilipsIntegrationService> logger,
            IServiceProvider serviceProvider)
        {
            _mqttService = mqttService;
            _hueService = hueService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting IoT-Philips Hue Integration Service...");

            // Aspetta un po' per permettere al sistema di inizializzarsi
            await Task.Delay(3000, stoppingToken);

            // Testa connessione Hue
            var hueConnected = await _hueService.TestConnectionAsync();
            if (!hueConnected)
            {
                _logger.LogWarning("Philips Hue emulator not available - visual feedback disabled");
                return;
            }

            // Inizializza le lampadine
            await _hueService.InitializeLightsAsync(20, 30); // 20 mezzi, 30 slot

            // Sottoscrivi ai topic MQTT per aggiornare le lampadine
            await SubscribeToMqttTopics();

            _logger.LogInformation("IoT-Philips Hue Integration Service started successfully");

            // Mantieni il servizio attivo
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);
            }
        }

        /// <summary>
        /// Sottoscrive ai topic MQTT per aggiornamenti real-time
        /// </summary>
        private async Task SubscribeToMqttTopics()
        {
            // Topic comandi attuatori (commands -> visual feedback)
            await _mqttService.SubscribeAsync("parking/+/stato_mezzi/+", message => OnMezzoStateChanged("parking/+/stato_mezzi/+", message));
            await _mqttService.SubscribeAsync("parking/+/attuatori/sblocco/+", message => OnMezzoUnlockCommand("parking/+/attuatori/sblocco/+", message));
            await _mqttService.SubscribeAsync("parking/+/attuatori/led/+", message => OnSlotLedCommand("parking/+/attuatori/led/+", message));
            await _mqttService.SubscribeAsync("parking/+/attuatori/allarme/+", message => OnMezzoAlarmCommand("parking/+/attuatori/allarme/+", message));

            // Topic sensori (sensor data -> visual feedback)
            await _mqttService.SubscribeAsync("parking/+/sensori/batteria/+", message => OnBatteryLevelReceived("parking/+/sensori/batteria/+", message));
            await _mqttService.SubscribeAsync("parking/+/sensori/slot/+/occupancy", message => OnSlotOccupancyChanged("parking/+/sensori/slot/+/occupancy", message));
            await _mqttService.SubscribeAsync("parking/+/dispositivi/+/heartbeat", message => OnDeviceHeartbeat("parking/+/dispositivi/+/heartbeat", message));

            _logger.LogInformation("Subscribed to MQTT topics for Hue integration");
        }

        /// <summary>
        /// Gestisce cambi di stato dei mezzi
        /// </summary>
        private async Task OnMezzoStateChanged(string topic, string message)
        {
            try
            {
                _logger.LogDebug("Processing mezzo state change: {Topic} -> {Message}", topic, message);

                var mezzoId = ExtractMezzoIdFromTopic(topic);
                if (mezzoId == null) return;

                var command = JsonSerializer.Deserialize<MezzoCommand>(message);
                if (command == null) return;

                var hueStatus = command.Action.ToLower() switch
                {
                    "unlock" => MezzoStatus.InUso,
                    "lock" => MezzoStatus.Disponibile,
                    "maintenance" => MezzoStatus.Manutenzione,
                    "alarm" => MezzoStatus.BatteriaBassa, // Usa arancione lampeggiante per allarmi
                    _ => MezzoStatus.Disponibile
                };

                await _hueService.SetMezzoStatusAsync(mezzoId.Value, hueStatus);
                _logger.LogInformation("Updated Hue light for mezzo {MezzoId}: {Status}", mezzoId, hueStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mezzo state change: {Topic}", topic);
            }
        }

        /// <summary>
        /// Gestisce comandi di sblocco mezzi
        /// </summary>
        private async Task OnMezzoUnlockCommand(string topic, string message)
        {
            try
            {
                var mezzoId = ExtractMezzoIdFromTopic(topic);
                if (mezzoId == null) return;

                await _hueService.SetMezzoStatusAsync(mezzoId.Value, MezzoStatus.InUso);
                _logger.LogInformation("Mezzo {MezzoId} unlocked - Hue updated", mezzoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mezzo unlock: {Topic}", topic);
            }
        }

        /// <summary>
        /// Gestisce comandi LED per slot
        /// </summary>
        private async Task OnSlotLedCommand(string topic, string message)
        {
            try
            {
                var slotId = ExtractSlotIdFromTopic(topic);
                if (slotId == null) return;

                var command = JsonSerializer.Deserialize<LedCommand>(message);
                if (command == null) return;

                var slotStatus = command.State.ToLower() switch
                {
                    "occupied" => SlotStatus.Occupato,
                    "reserved" => SlotStatus.Riservato,
                    "free" => SlotStatus.Libero,
                    "maintenance" => SlotStatus.Manutenzione,
                    _ => SlotStatus.Libero
                };

                await _hueService.SetSlotStatusAsync(slotId.Value, slotStatus);
                _logger.LogInformation("Updated slot {SlotId} LED: {Status}", slotId, slotStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing slot LED command: {Topic}", topic);
            }
        }

        /// <summary>
        /// Gestisce comandi allarme
        /// </summary>
        private async Task OnMezzoAlarmCommand(string topic, string message)
        {
            try
            {
                var mezzoId = ExtractMezzoIdFromTopic(topic);
                if (mezzoId == null) return;

                var command = JsonSerializer.Deserialize<AlarmCommand>(message);
                if (command == null) return;

                var status = command.Enabled ? MezzoStatus.BatteriaBassa : MezzoStatus.Disponibile;
                await _hueService.SetMezzoStatusAsync(mezzoId.Value, status);
                
                _logger.LogWarning("Alarm {Status} for mezzo {MezzoId}", 
                    command.Enabled ? "ACTIVATED" : "DEACTIVATED", mezzoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alarm command: {Topic}", topic);
            }
        }

        /// <summary>
        /// Gestisce dati batteria per alert visivi
        /// </summary>
        private async Task OnBatteryLevelReceived(string topic, string message)
        {
            try
            {
                var mezzoId = ExtractMezzoIdFromTopic(topic);
                if (mezzoId == null) return;

                var batteryData = JsonSerializer.Deserialize<BatteryData>(message);
                if (batteryData == null) return;

                // Batteria bassa? Mostra alert arancione lampeggiante
                if (batteryData.Level <= 15)
                {
                    await _hueService.SetMezzoStatusAsync(mezzoId.Value, MezzoStatus.BatteriaBassa);
                    _logger.LogWarning("Low battery alert for mezzo {MezzoId}: {Level}%", mezzoId, batteryData.Level);
                }
                else if (batteryData.Level > 20)
                {
                    // Batteria OK, torna a stato normale
                    await _hueService.SetMezzoStatusAsync(mezzoId.Value, MezzoStatus.Disponibile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error processing battery data: {Topic}", topic);
            }
        }

        /// <summary>
        /// Gestisce cambi occupazione slot
        /// </summary>
        private async Task OnSlotOccupancyChanged(string topic, string message)
        {
            try
            {
                var slotId = ExtractSlotIdFromTopic(topic);
                if (slotId == null) return;

                var occupancyData = JsonSerializer.Deserialize<SlotOccupancyData>(message);
                if (occupancyData == null) return;

                var status = occupancyData.IsOccupied ? SlotStatus.Occupato : SlotStatus.Libero;
                await _hueService.SetSlotStatusAsync(slotId.Value, status);

                _logger.LogDebug("🅿️ Slot {SlotId} {Status}", slotId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing slot occupancy: {Topic}", topic);
            }
        }

        /// <summary>
        /// Gestisce heartbeat dispositivi (per diagnostica)
        /// </summary>
        private async Task OnDeviceHeartbeat(string topic, string message)
        {
            try
            {
                var parcheggioId = ExtractParcheggioIdFromTopic(topic);
                if (parcheggioId == null) return;

                var heartbeat = JsonSerializer.Deserialize<DeviceHeartbeat>(message);
                if (heartbeat == null) return;

                // Se dispositivo offline, mostra alert
                var alertType = heartbeat.IsOnline ? AlertType.Normal : AlertType.Warning;
                await _hueService.SetParcheggioAlertAsync(parcheggioId.Value, alertType);

                if (!heartbeat.IsOnline)
                {
                    _logger.LogWarning("Device {DeviceId} offline in parcheggio {ParcheggioId}", 
                        heartbeat.DeviceId, parcheggioId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device heartbeat: {Topic}", topic);
            }
        }

        // ===== UTILITY METHODS =====

        private int? ExtractMezzoIdFromTopic(string topic)
        {
            var parts = topic.Split('/');
            if (parts.Length >= 4 && int.TryParse(parts[^1], out var mezzoId))
                return mezzoId;
            return null;
        }

        private int? ExtractSlotIdFromTopic(string topic)
        {
            var parts = topic.Split('/');
            if (parts.Length >= 4 && int.TryParse(parts[^2], out var slotId))
                return slotId;
            return null;
        }

        private int? ExtractParcheggioIdFromTopic(string topic)
        {
            var parts = topic.Split('/');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var parcheggioId))
                return parcheggioId;
            return null;
        }
    }

    // ===== DTO CLASSES =====

    public class MezzoCommand
    {
        public string Action { get; set; } = "";
        public int MezzoId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LedCommand
    {
        public string State { get; set; } = "";
        public string Color { get; set; } = "";
        public int Brightness { get; set; }
    }

    public class AlarmCommand
    {
        public bool Enabled { get; set; }
        public string Type { get; set; } = "";
        public int Duration { get; set; }
    }

    public class BatteryData
    {
        public int Level { get; set; }
        public double Voltage { get; set; }
        public string Status { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class SlotOccupancyData
    {
        public int SlotId { get; set; }
        public int ParkingId { get; set; }
        public bool IsOccupied { get; set; }
        public int? MezzoId { get; set; }
        public float Weight { get; set; }
        public bool SensorWorking { get; set; } = true;
        public DateTime LastChange { get; set; } = DateTime.UtcNow;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class DeviceHeartbeat
    {
        public string DeviceId { get; set; } = "";
        public int? MezzoId { get; set; }
        public int? SlotId { get; set; }
        public int ParkingId { get; set; }
        public string DeviceType { get; set; } = "";
        public bool IsOnline { get; set; } = true;
        public float SignalStrength { get; set; } = -50.0f;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}