namespace SharingMezzi.Core.Entities
{
    public enum StatoPagamento { InAttesa, Completato, Fallito, Rimborsato }
    public enum MetodoPagamento { CartaCredito, PayPal, Contanti }
    
    public class Pagamento : BaseEntity
    {
        public decimal Importo { get; set; }
        public StatoPagamento Stato { get; set; } = StatoPagamento.InAttesa;
        public MetodoPagamento Metodo { get; set; }
        public DateTime DataPagamento { get; set; }
        public string? TransactionId { get; set; }
        
        // Relazioni
        public int UtenteId { get; set; }
        public Utente Utente { get; set; } = null!;
        
        public int? CorsaId { get; set; }
        public Corsa? Corsa { get; set; }
    }
}