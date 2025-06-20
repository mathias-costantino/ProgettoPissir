using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Core.Interfaces.Services
{
    public interface ICorsaService
    {
        Task<CorsaDto> IniziaCorsa(IniziaCorsa comando);
        Task<CorsaDto> TerminaCorsa(int corsaId, TerminaCorsa comando);
        Task<IEnumerable<CorsaDto>> GetCorseUtente(int utenteId);
        Task<CorsaDto?> GetCorsaAttiva(int utenteId);
        Task<decimal> CalcolaCosto(int corsaId);
        
        // NUOVO: Controllo batteria per mezzi elettrici
        Task<(bool IsAcceptable, int? BatteryLevel)> CheckBatteryLevel(int mezzoId);
    }
}