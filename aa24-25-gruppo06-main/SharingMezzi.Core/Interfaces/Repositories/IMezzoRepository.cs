using SharingMezzi.Core.Entities;

namespace SharingMezzi.Core.Interfaces.Repositories
{
    public interface IMezzoRepository : IRepository<Mezzo>
    {
        Task<IEnumerable<Mezzo>> GetAvailableAsync();
        Task<IEnumerable<Mezzo>> GetByParcheggioAsync(int parcheggioId);
        Task<IEnumerable<Mezzo>> GetByTipoAsync(TipoMezzo tipo);
        Task UpdateBatteryLevelAsync(int mezzoId, int batteryLevel);
        Task UpdateStatusAsync(int mezzoId, StatoMezzo stato);
    }
}