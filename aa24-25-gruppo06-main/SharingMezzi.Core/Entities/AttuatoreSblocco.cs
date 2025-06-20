namespace SharingMezzi.Core.Entities
{
    public enum StatoAttuatore { Attivo, Inattivo, Errore }
    
    public class AttuatoreSblocco : BaseEntity
    {
        public string SerialNumber { get; set; } = string.Empty;
        public StatoAttuatore Stato { get; set; } = StatoAttuatore.Attivo;
        public DateTime UltimaAttivazione { get; set; }
          // Relazioni
        public Slot? Slot { get; set; }
    }
}