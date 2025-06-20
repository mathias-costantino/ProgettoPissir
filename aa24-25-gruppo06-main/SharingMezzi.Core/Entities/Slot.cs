namespace SharingMezzi.Core.Entities
{
    public enum StatoSlot { Libero, Occupato, Manutenzione }
    
    public class Slot : BaseEntity
    {
        public int Numero { get; set; }
        public StatoSlot Stato { get; set; } = StatoSlot.Libero;
        public DateTime DataUltimoAggiornamento { get; set; } = DateTime.UtcNow;
        
        // Relazioni
        public int ParcheggioId { get; set; }
        public Parcheggio Parcheggio { get; set; } = null!;
        
        public int? MezzoId { get; set; }
        public Mezzo? Mezzo { get; set; }
        
        public int? AttuatoreSbloccoId { get; set; }
        public AttuatoreSblocco? AttuatoreSblocco { get; set; }
    }
}