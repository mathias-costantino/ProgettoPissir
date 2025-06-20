namespace SharingMezzi.Core.DTOs
{
    public class CreateParcheggioDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Indirizzo { get; set; } = string.Empty;
        public int Capienza { get; set; }
    }
}