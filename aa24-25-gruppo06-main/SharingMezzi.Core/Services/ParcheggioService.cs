using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace SharingMezzi.Core.Services
{
    public class ParcheggioService : IParcheggioService
    {
        private readonly IRepository<Parcheggio> _parcheggioRepository;
        private readonly IMezzoRepository _mezzoRepository;
        private readonly ILogger<ParcheggioService> _logger;

        public ParcheggioService(
            IRepository<Parcheggio> parcheggioRepository,
            IMezzoRepository mezzoRepository,
            ILogger<ParcheggioService> logger)
        {
            _parcheggioRepository = parcheggioRepository;
            _mezzoRepository = mezzoRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<ParcheggioDto>> GetAllParcheggiAsync()
        {
            var parcheggi = await _parcheggioRepository.GetAllAsync();
            var result = new List<ParcheggioDto>();

            foreach (var parcheggio in parcheggi)
            {
                var mezzi = await _mezzoRepository.GetByParcheggioAsync(parcheggio.Id);
                var dto = MapToDto(parcheggio);
                dto.Mezzi = mezzi.Select(MapMezzoToDto).ToList();
                result.Add(dto);
            }

            return result;
        }

        public async Task<ParcheggioDto?> GetParcheggioByIdAsync(int id)
        {
            var parcheggio = await _parcheggioRepository.GetByIdAsync(id);
            if (parcheggio == null) return null;

            var mezzi = await _mezzoRepository.GetByParcheggioAsync(id);
            var dto = MapToDto(parcheggio);
            dto.Mezzi = mezzi.Select(MapMezzoToDto).ToList();
            
            return dto;
        }

        public async Task<ParcheggioDto> CreateParcheggioAsync(CreateParcheggioDto createDto)
        {
            var parcheggio = new Parcheggio
            {
                Nome = createDto.Nome,
                Indirizzo = createDto.Indirizzo,
                Capienza = createDto.Capienza,
                PostiLiberi = createDto.Capienza,
                PostiOccupati = 0
            };

            parcheggio = await _parcheggioRepository.AddAsync(parcheggio);
            _logger.LogInformation("Created new parcheggio {ParcheggioId}: {Nome}", parcheggio.Id, parcheggio.Nome);
            
            return MapToDto(parcheggio);
        }

        public async Task<ParcheggioDto> UpdateParcheggioAsync(int id, ParcheggioDto parcheggioDto)
        {
            var parcheggio = await _parcheggioRepository.GetByIdAsync(id);
            if (parcheggio == null)
                throw new ArgumentException($"Parcheggio {id} not found");

            parcheggio.Nome = parcheggioDto.Nome;
            parcheggio.Indirizzo = parcheggioDto.Indirizzo;
            parcheggio.Capienza = parcheggioDto.Capienza;

            parcheggio = await _parcheggioRepository.UpdateAsync(parcheggio);
            await UpdatePostiLiberiAsync(id);
            
            return MapToDto(parcheggio);
        }

        public async Task DeleteParcheggioAsync(int id)
        {
            await _parcheggioRepository.DeleteAsync(id);
            _logger.LogInformation("Deleted parcheggio {ParcheggioId}", id);
        }

        public async Task UpdatePostiLiberiAsync(int parcheggioId)
        {
            var parcheggio = await _parcheggioRepository.GetByIdAsync(parcheggioId);
            if (parcheggio == null) return;

            var mezzi = await _mezzoRepository.GetByParcheggioAsync(parcheggioId);
            
            // CORREZIONE: Conta solo i mezzi fisicamente presenti nel parcheggio
            // I mezzi in stato "Occupato" NON sono nel parcheggio (sono in uso nella città)
            var mezziPresentiNelParcheggio = mezzi.Count(m => 
                m.Stato == StatoMezzo.Disponibile || 
                m.Stato == StatoMezzo.Manutenzione || 
                m.Stato == StatoMezzo.Guasto);
            
            parcheggio.PostiOccupati = mezziPresentiNelParcheggio;
            parcheggio.PostiLiberi = parcheggio.Capienza - mezziPresentiNelParcheggio;
            
            await _parcheggioRepository.UpdateAsync(parcheggio);
            
            _logger.LogDebug("Updated parcheggio {ParcheggioId}: {PostiLiberi}/{Capienza} posti liberi, mezzi presenti: {MezziPresenti}", 
                parcheggioId, parcheggio.PostiLiberi, parcheggio.Capienza, mezziPresentiNelParcheggio);
        }

        private static ParcheggioDto MapToDto(Parcheggio parcheggio)
        {
            return new ParcheggioDto
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Indirizzo = parcheggio.Indirizzo,
                Capienza = parcheggio.Capienza,
                PostiLiberi = parcheggio.PostiLiberi,
                PostiOccupati = parcheggio.PostiOccupati,
                Mezzi = new List<MezzoDto>()
            };
        }

        private static MezzoDto MapMezzoToDto(Mezzo mezzo)
        {
            return new MezzoDto
            {
                Id = mezzo.Id,
                Modello = mezzo.Modello,
                Tipo = mezzo.Tipo.ToString(),
                IsElettrico = mezzo.IsElettrico,
                Stato = mezzo.Stato.ToString(),
                LivelloBatteria = mezzo.LivelloBatteria,
                TariffaPerMinuto = mezzo.TariffaPerMinuto
            };
        }
    }
}