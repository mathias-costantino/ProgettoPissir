using Microsoft.EntityFrameworkCore;
using SharingMezzi.Core.Entities;
using SharingMezzi.Core.Interfaces.Repositories;

namespace SharingMezzi.Infrastructure.Database.Repositories
{
    public class MezzoRepository : Repository<Mezzo>, IMezzoRepository
    {
        public MezzoRepository(SharingMezziContext context) : base(context) { }

        public async Task<IEnumerable<Mezzo>> GetAvailableAsync()
        {
            return await _dbSet
                .Where(m => m.Stato == StatoMezzo.Disponibile)
                .Include(m => m.Parcheggio)
                .Include(m => m.Slot)
                .ToListAsync();
        }

        public async Task<IEnumerable<Mezzo>> GetByParcheggioAsync(int parcheggioId)
        {
            return await _dbSet
                .Where(m => m.ParcheggioId == parcheggioId)
                .Include(m => m.Parcheggio)
                .Include(m => m.Slot)
                .ToListAsync();
        }

        public async Task<IEnumerable<Mezzo>> GetByTipoAsync(TipoMezzo tipo)
        {
            return await _dbSet
                .Where(m => m.Tipo == tipo)
                .Include(m => m.Parcheggio)
                .ToListAsync();
        }

        public async Task UpdateBatteryLevelAsync(int mezzoId, int batteryLevel)
        {
            var mezzo = await _dbSet.FindAsync(mezzoId);
            if (mezzo != null && mezzo.IsElettrico)
            {
                mezzo.LivelloBatteria = Math.Max(0, Math.Min(100, batteryLevel));
                mezzo.UpdatedAt = DateTime.UtcNow;
                
                // Auto-aggiorna stato se batteria critica
                if (batteryLevel < 10)
                {
                    mezzo.Stato = StatoMezzo.Manutenzione;
                }
                else if (batteryLevel >= 20 && mezzo.Stato == StatoMezzo.Manutenzione)
                {
                    mezzo.Stato = StatoMezzo.Disponibile;
                }
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateStatusAsync(int mezzoId, StatoMezzo stato)
        {
            var mezzo = await _dbSet.FindAsync(mezzoId);
            if (mezzo != null)
            {
                mezzo.Stato = stato;
                mezzo.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public override async Task<Mezzo?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(m => m.Parcheggio)
                .Include(m => m.Slot)
                .Include(m => m.Corse.Take(5))  // Ultime 5 corse
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public override async Task<IEnumerable<Mezzo>> GetAllAsync()
        {
            return await _dbSet
                .Include(m => m.Parcheggio)
                .Include(m => m.Slot)
                .ToListAsync();
        }
    }
}