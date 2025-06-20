using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.IoT.Handlers
{
    public class SensorHandler
    {
        private readonly IMezzoRepository _mezzoRepository;
        private readonly IParcheggioService _parcheggioService;
        private readonly IMqttService _mqttService;
        private readonly ILogger<SensorHandler> _logger;

        public SensorHandler(
            IMezzoRepository mezzoRepository, 
            IParcheggioService parcheggioService,
            IMqttService mqttService,
            ILogger<SensorHandler> logger)
        {
            _mezzoRepository = mezzoRepository;
            _parcheggioService = parcheggioService;
            _mqttService = mqttService;
            _logger = logger;
        }

        public async Task StartListening()
        {
            await _mqttService.ConnectAsync();
            
            // Subscribe ai topic sensori secondo specifica
            await _mqttService.SubscribeAsync("parking/+/mezzi", HandleParkingMezziMessage);
            await _mqttService.SubscribeAsync("parking/+/sensori/#", HandleSensorMessage);
            
            _logger.LogInformation("Sensor Handler started - listening to MQTT topics");
        }

        private async Task HandleParkingMezziMessage(string payload)
        {
            try
            {
                var message = JsonSerializer.Deserialize<SharingMezziMqttMessage>(payload);
                if (message != null && message.MezzoId.HasValue && message.BatteryLevel.HasValue)
                {
                    _logger.LogInformation("Received parking mezzi update: Mezzo {MezzoId}, Battery {Battery}%, Status {Status}", 
                        message.MezzoId, message.BatteryLevel, message.LockState);
                    
                    // Ottieni lo stato del mezzo prima dell'aggiornamento
                    var mezzoPreAggiornamento = await _mezzoRepository.GetByIdAsync(message.MezzoId.Value);
                    var statoOriginale = mezzoPreAggiornamento?.Stato;
                    
                    // Aggiorna database
                    await _mezzoRepository.UpdateBatteryLevelAsync(message.MezzoId.Value, message.BatteryLevel.Value);
                    
                    // Verifica se lo stato è cambiato e aggiorna il parcheggio
                    var mezzoPostAggiornamento = await _mezzoRepository.GetByIdAsync(message.MezzoId.Value);
                    if (mezzoPostAggiornamento != null && 
                        mezzoPostAggiornamento.ParcheggioId.HasValue &&
                        statoOriginale != mezzoPostAggiornamento.Stato)
                    {
                        await _parcheggioService.UpdatePostiLiberiAsync(mezzoPostAggiornamento.ParcheggioId.Value);
                        _logger.LogInformation("Updated parking {ParcheggioId} counts after IoT battery-triggered state change for mezzo {MezzoId}", 
                            mezzoPostAggiornamento.ParcheggioId.Value, message.MezzoId.Value);
                    }
                    
                    // Logica batteria scarica
                    if (message.BatteryLevel < 20)
                    {
                        await PublishLowBatteryAlert(message);
                    }
                    
                    // Logica stato critico
                    if (message.LockState == "error" || message.BatteryLevel < 5)
                    {
                        await PublishMaintenanceAlert(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling parking mezzi message");
            }
        }

        private async Task HandleSensorMessage(string payload)
        {
            try
            {
                // Generic sensor message handler
                _logger.LogDebug("Received sensor message: {Payload}", payload);
                
                // Parse tipo specifico di sensore dal topic
                // Implementa logica specifica per ogni tipo
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling sensor message");
            }
        }

        private async Task PublishLowBatteryAlert(SharingMezziMqttMessage message)
        {
            var notification = new
            {
                Level = "warning",
                Source = "sensor",
                Message = $"Batteria scarica per mezzo {message.MezzoId}: {message.BatteryLevel}%",
                Data = new { MezzoId = message.MezzoId, BatteryLevel = message.BatteryLevel }
            };
            
            await _mqttService.PublishAsync("parking/1/sistema/notifiche", notification);
        }

        private async Task PublishMaintenanceAlert(SharingMezziMqttMessage message)
        {
            var notification = new
            {
                Level = "error",
                Source = "sensor",
                Message = $"Manutenzione richiesta per mezzo {message.MezzoId}",
                Data = new { MezzoId = message.MezzoId, Status = message.LockState, BatteryLevel = message.BatteryLevel }
            };
            
            await _mqttService.PublishAsync("parking/1/sistema/maintenance", notification);
        }
    }
}