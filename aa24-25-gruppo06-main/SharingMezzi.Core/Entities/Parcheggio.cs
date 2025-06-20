namespace SharingMezzi.Core.Entities
{
    public class Parcheggio : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Indirizzo { get; set; } = string.Empty;
        public int Capienza { get; set; }
        public int PostiLiberi { get; set; }
        public int PostiOccupati { get; set; }
        
        // Relazioni
        public ICollection<Slot> Slots { get; set; } = new List<Slot>();
        public ICollection<Corsa> CorsePartenza { get; set; } = new List<Corsa>();
        public ICollection<Corsa> CorseDestinazione { get; set; } = new List<Corsa>();
        public ICollection<Mezzo> Mezzi { get; set; } = new List<Mezzo>();
    }
}
