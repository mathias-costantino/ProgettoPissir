using SharingMezzi.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SharingMezzi.Infrastructure.Services
{
    /// <summary>
    /// Implementazione del servizio per inviare comandi agli attuatori MQTT
    /// Collega il CorsaService al broker MQTT per gestire sblocco/blocco mezzi
    /// </summary>
    public class MqttActuatorService : IMqttActuatorService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MqttActuatorService> _logger;

        public MqttActuatorService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<MqttActuatorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Invia comando di sblocco mezzo via MQTT
        /// </summary>
        public async Task SendUnlockCommand(int mezzoId, int? corsaId = null)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Cerca il broker MQTT nei servizi registrati
                var brokerService = scope.ServiceProvider.GetService<SharingMezziBroker>();
                if (brokerService != null)
                {
                    await brokerService.SendUnlockCommand(mezzoId, corsaId);
                    _logger.LogInformation("Successfully sent unlock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                        mezzoId, corsaId);
                }
                else
                {
                    _logger.LogWarning("MQTT Broker service not found - cannot send unlock command for Mezzo {MezzoId}", 
                        mezzoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending unlock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                    mezzoId, corsaId);
                throw;
            }
        }

        /// <summary>
        /// Invia comando di blocco mezzo via MQTT
        /// </summary>
        public async Task SendLockCommand(int mezzoId, int? corsaId = null)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Cerca il broker MQTT nei servizi registrati
                var brokerService = scope.ServiceProvider.GetService<SharingMezziBroker>();
                if (brokerService != null)
                {
                    await brokerService.SendLockCommand(mezzoId, corsaId);
                    _logger.LogInformation("Successfully sent lock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                        mezzoId, corsaId);
                }
                else
                {
                    _logger.LogWarning("MQTT Broker service not found - cannot send lock command for Mezzo {MezzoId}", 
                        mezzoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending lock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                    mezzoId, corsaId);
                throw;
            }
        }

        /// <summary>
        /// Richiede controllo batteria via MQTT per mezzi elettrici
        /// </summary>
        public async Task<(bool IsAcceptable, int? BatteryLevel)> RequestBatteryCheck(int mezzoId, int timeoutSeconds = 10)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                var brokerService = scope.ServiceProvider.GetService<SharingMezziBroker>();
                if (brokerService != null)
                {
                    var batteryLevel = await brokerService.RequestBatteryStatus(mezzoId, timeoutSeconds);
                    _logger.LogInformation("Battery check for Mezzo {MezzoId}: {Level}%", mezzoId, batteryLevel);
                    
                    // Soglia minima: 20%
                    bool isAcceptable = batteryLevel >= 20;
                    return (isAcceptable, batteryLevel);
                }
                else
                {
                    _logger.LogWarning("MQTT Broker service not found - cannot check battery for Mezzo {MezzoId}", 
                        mezzoId);
                    return (true, null); // Fallback: permetti l'avvio
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting battery check for Mezzo {MezzoId}", mezzoId);
                return (true, null); // Fallback: permetti l'avvio in caso di errore
            }
        }
    }
}
