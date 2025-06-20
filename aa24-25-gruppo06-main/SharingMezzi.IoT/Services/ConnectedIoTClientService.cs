using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.IoT.Clients;

namespace SharingMezzi.IoT.Services
{
    /// <summary>
    /// Servizio per gestire tutti i client IoT connessi
    /// Simula dispositivi montati sui mezzi del sistema
    /// </summary>
    public class ConnectedIoTClientsService
    {
        private readonly ILogger<ConnectedIoTClientsService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        
        private readonly List<MezzoIoTClient> _connectedClients = new();
        private readonly object _clientsLock = new();

        public ConnectedIoTClientsService(
            ILogger<ConnectedIoTClientsService> logger,
            IServiceScopeFactory serviceScopeFactory,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Inizializza client IoT per tutti i mezzi elettrici nel sistema
        /// </summary>
        public async Task InitializeIoTClientsAsync()
        {
            _logger.LogInformation("Initializing IoT clients for all mezzi...");
            
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var mezzoRepository = scope.ServiceProvider.GetRequiredService<IMezzoRepository>();
                
                // Recupera tutti i mezzi dal database
                var mezzi = await mezzoRepository.GetAllAsync();
                
                foreach (var mezzo in mezzi)
                {
                    if (IsClientExists(mezzo.Id))
                    {
                        _logger.LogWarning("IoT client for Mezzo {MezzoId} already exists", mezzo.Id);
                        continue;
                    }

                    await CreateIoTClientAsync(mezzo);
                }
                
                _logger.LogInformation("Initialized {Count} IoT clients", _connectedClients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing IoT clients");
            }
        }

        /// <summary>
        /// Crea e connette client IoT per un mezzo specifico
        /// </summary>
        public async Task<bool> CreateIoTClientAsync(SharingMezzi.Core.Entities.Mezzo mezzo)
        {
            try
            {
                if (mezzo.Id <= 0)
                {
                    _logger.LogError("Invalid mezzo ID: {MezzoId}", mezzo.Id);
                    return false;
                }

                var clientLogger = _loggerFactory.CreateLogger<MezzoIoTClient>();
                var client = new MezzoIoTClient(mezzo.Id, clientLogger);
                
                // Configura stato iniziale basato su dati mezzo PRIMA dell'inizializzazione
                client.State.IsElettrico = mezzo.IsElettrico;
                client.State.ParkingId = mezzo.ParcheggioId ?? 1;
                
                // Configura livello batteria con validazione rigorosa
                if (mezzo.IsElettrico)
                {
                    var batteryLevel = mezzo.LivelloBatteria ?? 85;
                    client.State.BatteryLevel = Math.Max(0, Math.Min(100, batteryLevel));
                }
                else
                {
                    client.State.BatteryLevel = 100; // Mezzi non elettrici hanno sempre 100% per compatibilità
                }
                
                // Validazione finale dello stato per evitare messaggi con valori null
                if (client.State.MezzoId <= 0)
                {
                    _logger.LogError("Invalid MezzoId in client state: {MezzoId}", client.State.MezzoId);
                    client.Dispose();
                    return false;
                }
                
                if (client.State.ParkingId <= 0)
                {
                    _logger.LogWarning("Invalid ParkingId for Mezzo {MezzoId}: {ParkingId}, setting to 1", 
                        mezzo.Id, client.State.ParkingId);
                    client.State.ParkingId = 1;
                }
                
                _logger.LogDebug("Pre-init state - Mezzo {MezzoId}: ParkingId={ParkingId}, BatteryLevel={BatteryLevel}%, Electric={IsElettrico}", 
                    client.State.MezzoId, client.State.ParkingId, client.State.BatteryLevel, client.State.IsElettrico);
                
                var connected = await client.InitializeAsync();
                if (connected)
                {
                    lock (_clientsLock)
                    {
                        _connectedClients.Add(client);
                    }
                    
                    _logger.LogInformation("Created IoT client for Mezzo {MezzoId} ({Tipo}) - Battery: {BatteryLevel}%, Electric: {IsElettrico}, Parking: {ParkingId}", 
                        mezzo.Id, mezzo.Tipo, client.State.BatteryLevel, client.State.IsElettrico, client.State.ParkingId);
                    
                    return true;
                }
                else
                {
                    client.Dispose();
                    _logger.LogError("Failed to connect IoT client for Mezzo {MezzoId}", mezzo.Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating IoT client for Mezzo {MezzoId}", mezzo.Id);
                return false;
            }
        }

        /// <summary>
        /// Rimuove client IoT per un mezzo
        /// </summary>
        public async Task<bool> RemoveIoTClientAsync(int mezzoId)
        {
            lock (_clientsLock)
            {
                var client = _connectedClients.FirstOrDefault(c => c.State.MezzoId == mezzoId);
                if (client != null)
                {
                    _connectedClients.Remove(client);
                    client.Dispose();
                    
                    _logger.LogInformation("Removed IoT client for Mezzo {MezzoId}", mezzoId);
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Ottiene client IoT per un mezzo specifico
        /// </summary>
        public MezzoIoTClient? GetIoTClient(int mezzoId)
        {
            lock (_clientsLock)
            {
                return _connectedClients.FirstOrDefault(c => c.State.MezzoId == mezzoId);
            }
        }

        /// <summary>
        /// Ottiene tutti i client IoT connessi
        /// </summary>
        public List<MezzoIoTClient> GetAllConnectedClients()
        {
            lock (_clientsLock)
            {
                return new List<MezzoIoTClient>(_connectedClients);
            }
        }

        /// <summary>
        /// Verifica se esiste già un client per il mezzo
        /// </summary>
        public bool IsClientExists(int mezzoId)
        {
            lock (_clientsLock)
            {
                return _connectedClients.Any(c => c.State.MezzoId == mezzoId);
            }
        }

        /// <summary>
        /// Ottiene statistiche client connessi
        /// </summary>
        public object GetConnectionStats()
        {
            lock (_clientsLock)
            {
                var totalClients = _connectedClients.Count;
                var connectedClients = _connectedClients.Count(c => c.IsConnected);
                var movingClients = _connectedClients.Count(c => c.State.IsMoving);
                var lowBatteryClients = _connectedClients.Count(c => c.State.IsElettrico && c.State.BatteryLevel < 20);

                return new
                {
                    TotalClients = totalClients,
                    ConnectedClients = connectedClients,
                    MovingClients = movingClients,
                    LowBatteryClients = lowBatteryClients,
                    ConnectionRate = totalClients > 0 ? (double)connectedClients / totalClients * 100 : 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Simula guasto per un mezzo (per testing)
        /// </summary>
        public async Task SimulateDeviceError(int mezzoId, string errorMessage)
        {
            var client = GetIoTClient(mezzoId);
            if (client != null)
            {
                _logger.LogWarning("Simulating device error for Mezzo {MezzoId}: {Error}", 
                    mezzoId, errorMessage);
            }
        }

        /// <summary>
        /// Cleanup di tutti i client
        /// </summary>
        public void DisposeAllClients()
        {
            _logger.LogInformation("Disposing all IoT clients...");
            
            lock (_clientsLock)
            {
                foreach (var client in _connectedClients)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing IoT client for Mezzo {MezzoId}", 
                            client.State.MezzoId);
                    }
                }
                
                _connectedClients.Clear();
            }
            
            _logger.LogInformation("All IoT clients disposed");
        }
    }
}