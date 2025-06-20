using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Core.Interfaces.Services
{
    public interface IParcheggioService
    {
        Task<IEnumerable<ParcheggioDto>> GetAllParcheggiAsync();
        Task<ParcheggioDto?> GetParcheggioByIdAsync(int id);
        Task<ParcheggioDto> CreateParcheggioAsync(CreateParcheggioDto createDto);
        Task<ParcheggioDto> UpdateParcheggioAsync(int id, ParcheggioDto parcheggioDto);
        Task DeleteParcheggioAsync(int id);
        Task UpdatePostiLiberiAsync(int parcheggioId);
    }
}