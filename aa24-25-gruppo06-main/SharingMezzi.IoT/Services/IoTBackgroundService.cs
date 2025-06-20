using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharingMezzi.IoT.Services
{
    /// <summary>
    /// Servizio background per gestire il sistema IoT
    /// Avvia broker MQTT e inizializza client simulati
    /// </summary>
    public class IoTBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IoTBackgroundService> _logger;

        public IoTBackgroundService(IServiceProvider serviceProvider, ILogger<IoTBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting IoT Background Service...");
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var connectedClientsService = scope.ServiceProvider.GetRequiredService<ConnectedIoTClientsService>();
                
                // Aspetta che il database sia pronto
                await Task.Delay(5000, stoppingToken);
                
                // Inizializza tutti i client IoT
                await connectedClientsService.InitializeIoTClientsAsync();
                
                _logger.LogInformation("IoT Background Service started successfully");
                
                // Mantieni il servizio attivo e monitora lo stato
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(30000, stoppingToken); // Check ogni 30 secondi
                    
                    var stats = connectedClientsService.GetConnectionStats();
                    _logger.LogDebug("IoT Stats: {Stats}", stats);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("IoT Background Service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IoT Background Service");
            }
            finally
            {
                _logger.LogInformation("IoT Background Service stopped");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping IoT Background Service...");
            
            using var scope = _serviceProvider.CreateScope();
            var connectedClientsService = scope.ServiceProvider.GetRequiredService<ConnectedIoTClientsService>();
            connectedClientsService.DisposeAllClients();
            
            await base.StopAsync(cancellationToken);
        }
    }
}
