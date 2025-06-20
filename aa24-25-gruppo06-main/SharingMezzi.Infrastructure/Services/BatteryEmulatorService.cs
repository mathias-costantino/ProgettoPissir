using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Entities;
using System.Text.Json;

namespace SharingMezzi.Infrastructure.Services
{
    /// <summary>
    /// Servizio di emulazione batteria per testare il controllo del livello di batteria
    /// Simula il comportamento di dispositivi IoT sui mezzi elettrici
    /// </summary>
    public class BatteryEmulatorService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BatteryEmulatorService> _logger;
        private readonly Dictionary<int, BatteryEmulatorState> _batteryStates;
        private readonly Timer _updateTimer;

        public BatteryEmulatorService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BatteryEmulatorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _batteryStates = new Dictionary<int, BatteryEmulatorState>();
            
            // Aggiorna ogni 30 secondi
            _updateTimer = new Timer(UpdateBatteryLevels, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Battery Emulator Service started");
            
            try
            {
                // Inizializza stati batteria per mezzi elettrici
                await InitializeBatteryStates();
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normale shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Battery Emulator Service");
            }
            finally
            {
                _logger.LogInformation("Battery Emulator Service stopped");
            }
        }

        /// <summary>
        /// Inizializza stati batteria per tutti i mezzi elettrici nel database
        /// </summary>
        private async Task InitializeBatteryStates()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var mezzoRepository = scope.ServiceProvider.GetRequiredService<IMezzoRepository>();
                
                var mezziElettrici = (await mezzoRepository.GetAllAsync())
                    .Where(m => m.IsElettrico && m.Stato != StatoMezzo.Manutenzione)
                    .ToList();

                foreach (var mezzo in mezziElettrici)
                {
                    if (!_batteryStates.ContainsKey(mezzo.Id))
                    {
                        var randomBattery = new Random().Next(15, 100);
                        _batteryStates[mezzo.Id] = new BatteryEmulatorState
                        {
                            MezzoId = mezzo.Id,
                            BatteryLevel = randomBattery,
                            IsCharging = randomBattery < 30,
                            LastUpdate = DateTime.UtcNow,
                            ChargingRate = new Random().Next(1, 3), // 1-3% per ciclo
                            DischargingRate = new Random().Next(1, 2) // 1-2% per ciclo
                        };
                        
                        _logger.LogInformation("Initialized battery for Mezzo {MezzoId}: {Level}% (Charging: {IsCharging})", 
                            mezzo.Id, randomBattery, randomBattery < 30);
                    }
                }
                
                _logger.LogInformation("Battery states initialized for {Count} electric vehicles", _batteryStates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing battery states");
            }
        }

        /// <summary>
        /// Aggiorna periodicamente i livelli di batteria simulati
        /// </summary>
        private async void UpdateBatteryLevels(object? state)
        {
            try
            {
                var updatedCount = 0;
                
                foreach (var kvp in _batteryStates.ToList())
                {
                    var mezzoId = kvp.Key;
                    var batteryState = kvp.Value;
                    
                    var oldLevel = batteryState.BatteryLevel;
                    
                    // Simula carica/scarica
                    if (batteryState.IsCharging)
                    {
                        batteryState.BatteryLevel = Math.Min(100, batteryState.BatteryLevel + batteryState.ChargingRate);
                        
                        // Smette di caricare a 90%
                        if (batteryState.BatteryLevel >= 90)
                        {
                            batteryState.IsCharging = false;
                        }
                    }
                    else
                    {
                        // Scarica solo se il mezzo è in uso
                        if (await IsMezzoInUse(mezzoId))
                        {
                            batteryState.BatteryLevel = Math.Max(0, batteryState.BatteryLevel - batteryState.DischargingRate);
                        }
                        
                        // Inizia a caricare se sotto il 25%
                        if (batteryState.BatteryLevel <= 25)
                        {
                            batteryState.IsCharging = true;
                        }
                    }
                    
                    batteryState.LastUpdate = DateTime.UtcNow;
                    
                    if (oldLevel != batteryState.BatteryLevel)
                    {
                        updatedCount++;
                        _logger.LogDebug("Mezzo {MezzoId}: {OldLevel}% → {NewLevel}% (Charging: {IsCharging})", 
                            mezzoId, oldLevel, batteryState.BatteryLevel, batteryState.IsCharging);
                    }
                }
                
                if (updatedCount > 0)
                {
                    _logger.LogInformation("Updated battery levels for {Count} vehicles", updatedCount);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating battery levels");
            }
        }

        /// <summary>
        /// Verifica se un mezzo è attualmente in uso (ha una corsa attiva)
        /// </summary>
        private async Task<bool> IsMezzoInUse(int mezzoId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var corsaRepository = scope.ServiceProvider.GetRequiredService<IRepository<Corsa>>();
                
                var corseAttive = await corsaRepository.GetAllAsync();
                return corseAttive.Any(c => c.MezzoId == mezzoId && c.Stato == StatoCorsa.InCorso);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ottiene lo stato batteria di un mezzo specifico
        /// </summary>
        public BatteryStatusDto? GetBatteryStatus(int mezzoId)
        {
            if (_batteryStates.TryGetValue(mezzoId, out var state))
            {
                return new BatteryStatusDto
                {
                    MezzoId = mezzoId,
                    BatteryLevel = state.BatteryLevel,
                    IsCharging = state.IsCharging,
                    Timestamp = state.LastUpdate,
                    ParkingId = 1, // Assumo parking 1
                    Status = state.BatteryLevel >= 20 ? "OK" : "LOW"
                };
            }
            
            return null;
        }

        /// <summary>
        /// Ottiene tutti gli stati batteria
        /// </summary>
        public IEnumerable<BatteryStatusDto> GetAllBatteryStatuses()
        {
            return _batteryStates.Values.Select(state => new BatteryStatusDto
            {
                MezzoId = state.MezzoId,
                BatteryLevel = state.BatteryLevel,
                IsCharging = state.IsCharging,
                Timestamp = state.LastUpdate,
                ParkingId = 1,
                Status = state.BatteryLevel >= 20 ? "OK" : "LOW"
            });
        }

        /// <summary>
        /// Forza aggiornamento livello batteria per testing
        /// </summary>
        public void SetBatteryLevel(int mezzoId, int level)
        {
            if (_batteryStates.TryGetValue(mezzoId, out var state))
            {
                state.BatteryLevel = Math.Max(0, Math.Min(100, level));
                state.LastUpdate = DateTime.UtcNow;
                state.IsCharging = level < 30;
                
                _logger.LogInformation("Manually set battery for Mezzo {MezzoId}: {Level}%", mezzoId, level);
            }
        }

        public override void Dispose()
        {
            _updateTimer?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Stato interno dell'emulatore batteria
    /// </summary>
    public class BatteryEmulatorState
    {
        public int MezzoId { get; set; }
        public int BatteryLevel { get; set; }
        public bool IsCharging { get; set; }
        public DateTime LastUpdate { get; set; }
        public int ChargingRate { get; set; } // % per ciclo
        public int DischargingRate { get; set; } // % per ciclo
    }
}
