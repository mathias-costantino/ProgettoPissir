using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace SharingMezzi.Core.Services
{
    public class MezzoService : IMezzoService
    {
        private readonly IMezzoRepository _mezzoRepository;
        private readonly IParcheggioService _parcheggioService;
        private readonly ILogger<MezzoService> _logger;

        public MezzoService(IMezzoRepository mezzoRepository, IParcheggioService parcheggioService, ILogger<MezzoService> logger)
        {
            _mezzoRepository = mezzoRepository;
            _parcheggioService = parcheggioService;
            _logger = logger;
        }

        public async Task<IEnumerable<MezzoDto>> GetAllMezziAsync()
        {
            var mezzi = await _mezzoRepository.GetAllAsync();
            return mezzi.Select(MapToDto);
        }

        public async Task<IEnumerable<MezzoDto>> GetMezziDisponibiliAsync()
        {
            var mezzi = await _mezzoRepository.GetAvailableAsync();
            return mezzi.Select(MapToDto);
        }

        public async Task<MezzoDto?> GetMezzoByIdAsync(int id)
        {
            var mezzo = await _mezzoRepository.GetByIdAsync(id);
            return mezzo != null ? MapToDto(mezzo) : null;
        }

        public async Task<IEnumerable<MezzoDto>> GetMezziByParcheggioAsync(int parcheggioId)
        {
            var mezzi = await _mezzoRepository.GetByParcheggioAsync(parcheggioId);
            return mezzi.Select(MapToDto);
        }

        public async Task<MezzoDto> CreateMezzoAsync(CreateMezzoDto createDto)
        {            var mezzo = new Mezzo
            {
                Modello = createDto.Modello,
                Tipo = Enum.Parse<TipoMezzo>(createDto.Tipo),
                IsElettrico = createDto.IsElettrico,
                TariffaPerMinuto = createDto.TariffaPerMinuto,
                TariffaFissa = 1.00m, // Tariffa fissa standard
                ParcheggioId = createDto.ParcheggioId,
                SlotId = createDto.SlotId,
                LivelloBatteria = createDto.IsElettrico ? 100 : null,
                Stato = StatoMezzo.Disponibile
            };

            mezzo = await _mezzoRepository.AddAsync(mezzo);
            _logger.LogInformation("Created new mezzo {MezzoId}: {Modello}", mezzo.Id, mezzo.Modello);
            
            return MapToDto(mezzo);
        }

        public async Task<MezzoDto> UpdateMezzoAsync(int id, MezzoDto mezzoDto)
        {
            var mezzo = await _mezzoRepository.GetByIdAsync(id);
            if (mezzo == null)
                throw new ArgumentException($"Mezzo {id} not found");

            var statoOriginale = mezzo.Stato;
            var parcheggioOriginale = mezzo.ParcheggioId;

            mezzo.Modello = mezzoDto.Modello;
            mezzo.TariffaPerMinuto = mezzoDto.TariffaPerMinuto;
            mezzo.ParcheggioId = mezzoDto.ParcheggioId;
            mezzo.SlotId = mezzoDto.SlotId;
            
            if (Enum.TryParse<StatoMezzo>(mezzoDto.Stato, out var stato))
                mezzo.Stato = stato;

            mezzo = await _mezzoRepository.UpdateAsync(mezzo);
            
            // Aggiorna contatori parcheggi se necessario
            bool statoChanged = statoOriginale != mezzo.Stato;
            bool parcheggioChanged = parcheggioOriginale != mezzo.ParcheggioId;
            
            if (statoChanged || parcheggioChanged)
            {
                // Aggiorna il parcheggio originale se il mezzo è stato spostato
                if (parcheggioChanged && parcheggioOriginale.HasValue)
                {
                    await _parcheggioService.UpdatePostiLiberiAsync(parcheggioOriginale.Value);
                }
                
                // Aggiorna il parcheggio attuale
                if (mezzo.ParcheggioId.HasValue)
                {
                    await _parcheggioService.UpdatePostiLiberiAsync(mezzo.ParcheggioId.Value);
                }
                
                _logger.LogInformation("Updated parking counts after mezzo {MezzoId} modification", id);
            }
            
            return MapToDto(mezzo);
        }

        public async Task DeleteMezzoAsync(int id)
        {
            await _mezzoRepository.DeleteAsync(id);
            _logger.LogInformation("Deleted mezzo {MezzoId}", id);
        }

        public async Task UpdateBatteryAsync(int mezzoId, int batteryLevel)
        {
            // Ottieni lo stato del mezzo prima dell'aggiornamento
            var mezzoPreAggiornamento = await _mezzoRepository.GetByIdAsync(mezzoId);
            var statoOriginale = mezzoPreAggiornamento?.Stato;
            
            await _mezzoRepository.UpdateBatteryLevelAsync(mezzoId, batteryLevel);
            
            // Verifica se lo stato è cambiato e aggiorna il parcheggio se necessario
            var mezzoPostAggiornamento = await _mezzoRepository.GetByIdAsync(mezzoId);
            if (mezzoPostAggiornamento != null && 
                mezzoPostAggiornamento.ParcheggioId.HasValue &&
                statoOriginale != mezzoPostAggiornamento.Stato)
            {
                await _parcheggioService.UpdatePostiLiberiAsync(mezzoPostAggiornamento.ParcheggioId.Value);
                _logger.LogInformation("Updated parking {ParcheggioId} counts after battery-triggered state change for mezzo {MezzoId}: {StatoOriginale} -> {NuovoStato}", 
                    mezzoPostAggiornamento.ParcheggioId.Value, mezzoId, statoOriginale, mezzoPostAggiornamento.Stato);
            }
            
            _logger.LogDebug("Updated battery for mezzo {MezzoId}: {BatteryLevel}%", mezzoId, batteryLevel);
        }

        public async Task<bool> IsMezzoAvailableAsync(int mezzoId)
        {
            var mezzo = await _mezzoRepository.GetByIdAsync(mezzoId);
            return mezzo?.Stato == StatoMezzo.Disponibile;
        }

        private static MezzoDto MapToDto(Mezzo mezzo)
        {            return new MezzoDto
            {
                Id = mezzo.Id,
                Modello = mezzo.Modello,
                Tipo = mezzo.Tipo.ToString(),
                IsElettrico = mezzo.IsElettrico,
                Stato = mezzo.Stato.ToString(),
                LivelloBatteria = mezzo.LivelloBatteria,
                TariffaPerMinuto = mezzo.TariffaPerMinuto,
                TariffaFissa = mezzo.TariffaFissa,
                UltimaManutenzione = mezzo.UltimaManutenzione,
                ParcheggioId = mezzo.ParcheggioId,
                SlotId = mezzo.SlotId
            };
        }
    }
}