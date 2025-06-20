using System.ComponentModel.DataAnnotations.Schema;

namespace SharingMezzi.Core.Entities
{
    public enum TipoMezzo { BiciMuscolare, BiciElettrica, Monopattino }
    public enum StatoMezzo { Disponibile, Occupato, Manutenzione, Guasto }

    public class Mezzo : BaseEntity
    {
        public string Modello { get; set; } = string.Empty;
        public TipoMezzo Tipo { get; set; }
        public bool IsElettrico { get; set; }
        public StatoMezzo Stato { get; set; } = StatoMezzo.Disponibile;        public int? LivelloBatteria { get; set; } // null per mezzi non elettrici
        public decimal TariffaPerMinuto { get; set; }
        public decimal TariffaFissa { get; set; } = 1.00m; // Tariffa fissa di attivazione
        public DateTime? UltimaManutenzione { get; set; }
        
        // Relazioni
        public int? ParcheggioId { get; set; }
        public Parcheggio? Parcheggio { get; set; }
        
        public int? SlotId { get; set; }
        public Slot? Slot { get; set; }
        
        public ICollection<Corsa> Corse { get; set; } = new List<Corsa>();
        public ICollection<SensoreBatteria> SensoriBatteria { get; set; } = new List<SensoreBatteria>();
        public ICollection<AttuatoreSblocco> AttuatoriSblocco { get; set; } = new List<AttuatoreSblocco>();
    }
}