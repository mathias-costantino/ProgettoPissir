namespace SharingMezzi.Core.DTOs;

public class PagamentoDto
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public int CorsaId { get; set; }
    public decimal Importo { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty;
    public DateTime DataPagamento { get; set; }
    public string Stato { get; set; } = string.Empty;
    
    // Dati aggiuntivi per visualizzazione
    public string? NomeUtente { get; set; }
    public string? DescrizioneCorsa { get; set; }
}
