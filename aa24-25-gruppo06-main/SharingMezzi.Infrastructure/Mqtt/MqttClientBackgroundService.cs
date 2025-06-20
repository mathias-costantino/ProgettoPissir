using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Infrastructure.Mqtt
{
    /// <summary>
    /// Servizio background che mantiene attiva la connessione del client MQTT al broker embedded
    /// </summary>
    public class MqttClientBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttClientBackgroundService> _logger;

        public MqttClientBackgroundService(IServiceProvider serviceProvider, ILogger<MqttClientBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting MQTT Client Background Service...");
            
            // Aspetta che il broker embedded sia avviato
            await Task.Delay(5000, stoppingToken);
            
            using var scope = _serviceProvider.CreateScope();
            var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();
            
            try
            {
                // Connetti al broker MQTT embedded
                _logger.LogInformation("Attempting to connect to embedded MQTT broker...");
                await mqttService.ConnectAsync();
                _logger.LogInformation("Connected to embedded MQTT broker");
                
                // Mantieni la connessione attiva
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!mqttService.IsConnected)
                    {
                        _logger.LogWarning("MQTT connection lost, attempting reconnect...");
                        try
                        {
                            await mqttService.ConnectAsync();
                            _logger.LogInformation("Reconnected to MQTT broker");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to reconnect to MQTT broker");
                        }
                    }
                    
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MQTT Client Background Service");
            }
            finally
            {
                try
                {
                    await mqttService.DisconnectAsync();
                    _logger.LogInformation("MQTT Client Background Service stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting MQTT client");
                }
            }
        }
    }
}
