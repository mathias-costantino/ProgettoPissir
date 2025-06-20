namespace SharingMezzi.Core.DTOs
{
    public class CreateMezzoDto
    {
        public string Modello { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // "BiciMuscolare", "BiciElettrica", "Monopattino"
        public bool IsElettrico { get; set; }
        public decimal TariffaPerMinuto { get; set; }
        public decimal TariffaFissa { get; set; } = 1.00m;
        public int? ParcheggioId { get; set; }
        public int? SlotId { get; set; }
    }
}