namespace SharingMezzi.Core.DTOs
{
    public class ParcheggioDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Indirizzo { get; set; } = string.Empty;
        public int Capienza { get; set; }
        public int PostiLiberi { get; set; }
        public int PostiOccupati { get; set; }
        public List<MezzoDto> Mezzi { get; set; } = new();
    }
}