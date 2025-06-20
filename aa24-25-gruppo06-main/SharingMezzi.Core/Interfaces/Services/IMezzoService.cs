using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Core.Interfaces.Services
{
    public interface IMezzoService
    {
        Task<IEnumerable<MezzoDto>> GetAllMezziAsync();
        Task<IEnumerable<MezzoDto>> GetMezziDisponibiliAsync();
        Task<MezzoDto?> GetMezzoByIdAsync(int id);
        Task<IEnumerable<MezzoDto>> GetMezziByParcheggioAsync(int parcheggioId);
        Task<MezzoDto> CreateMezzoAsync(CreateMezzoDto createDto);
        Task<MezzoDto> UpdateMezzoAsync(int id, MezzoDto mezzoDto);
        Task DeleteMezzoAsync(int id);
        Task UpdateBatteryAsync(int mezzoId, int batteryLevel);
        Task<bool> IsMezzoAvailableAsync(int mezzoId);
    }
}