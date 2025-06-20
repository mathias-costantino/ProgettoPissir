using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.IoT.Handlers
{
    public class ActuatorHandler
    {
        private readonly IMqttService _mqttService;
        private readonly ILogger<ActuatorHandler> _logger;

        public ActuatorHandler(IMqttService mqttService, ILogger<ActuatorHandler> logger)
        {
            _mqttService = mqttService;
            _logger = logger;
        }

        public async Task StartListening()
        {
            // Listen for commands from the API to control actuators
            await _mqttService.SubscribeAsync("parking/+/attuatori/#", HandleActuatorCommand);
            
            _logger.LogInformation("Actuator Handler started - ready to process commands");
        }

        public async Task SendUnlockCommand(int mezzoId, int? corsaId = null)
        {
            var command = new
            {
                MezzoId = mezzoId,
                Action = "unlock",
                CorsaId = corsaId,
                Timestamp = DateTime.UtcNow
            };
            
            var topic = $"parking/1/stato_mezzi/{mezzoId}";
            await _mqttService.PublishAsync(topic, command);
            
            _logger.LogInformation("Sent unlock command for Mezzo {MezzoId}", mezzoId);
        }

        public async Task SendLockCommand(int mezzoId)
        {
            var command = new
            {
                MezzoId = mezzoId,
                Action = "lock",
                Timestamp = DateTime.UtcNow
            };
            
            var topic = $"parking/1/stato_mezzi/{mezzoId}";
            await _mqttService.PublishAsync(topic, command);
            
            _logger.LogInformation("Sent lock command for Mezzo {MezzoId}", mezzoId);
        }

        public async Task SendLedCommand(int slotId, string color, string pattern = "solid")
        {
            var command = new
            {
                SlotId = slotId,
                Color = color,
                Pattern = pattern,
                Timestamp = DateTime.UtcNow
            };
            
            var topic = $"parking/1/attuatori/led/{slotId}";
            await _mqttService.PublishAsync(topic, command);
            
            _logger.LogInformation("Sent LED command for Slot {SlotId}: {Color} {Pattern}", slotId, color, pattern);
        }

        private async Task HandleActuatorCommand(string payload)
        {
            try
            {
                _logger.LogDebug("Received actuator command: {Payload}", payload);
                
                // Echo back confirmation if needed
                var confirmation = new
                {
                    Status = "received",
                    Timestamp = DateTime.UtcNow,
                    Payload = payload
                };
                
                await _mqttService.PublishAsync("parking/1/sistema/notifiche", confirmation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling actuator command");
            }
        }
    }
}