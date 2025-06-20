namespace SharingMezzi.Core.DTOs
{
    public class MezzoDto
    {
        public int Id { get; set; }
        public string Modello { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool IsElettrico { get; set; }
        public string Stato { get; set; } = string.Empty;        public int? LivelloBatteria { get; set; }
        public decimal TariffaPerMinuto { get; set; }
        public decimal TariffaFissa { get; set; }
        public DateTime? UltimaManutenzione { get; set; }
        public int? ParcheggioId { get; set; }
        public int? SlotId { get; set; }
    }
}