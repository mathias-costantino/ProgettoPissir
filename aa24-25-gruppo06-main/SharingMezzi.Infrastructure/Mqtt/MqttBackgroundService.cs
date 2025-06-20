using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Infrastructure.Mqtt
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttBackgroundService> _logger;

        public MqttBackgroundService(IServiceProvider serviceProvider, ILogger<MqttBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting MQTT Background Service...");
            
            // Aspetta che il broker sia pronto
            await Task.Delay(3000, stoppingToken);
            
            using var scope = _serviceProvider.CreateScope();
            var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();
            
            try
            {
                // Connetti al broker MQTT
                _logger.LogInformation("Attempting to connect to MQTT broker...");
                await mqttService.ConnectAsync();
                _logger.LogInformation("Connected to MQTT broker");
                
                // Mantieni la connessione attiva
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!mqttService.IsConnected)
                    {
                        _logger.LogWarning("MQTT connection lost, attempting reconnect...");
                        await mqttService.ConnectAsync();
                    }
                    
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MQTT Background Service");
            }
            finally
            {
                await mqttService.DisconnectAsync();
                _logger.LogInformation("MQTT Background Service stopped");
            }
        }
    }
}