namespace SharingMezzi.Core.Entities
{
    public class Ricarica : BaseEntity
    {
        public decimal Importo { get; set; }
        public MetodoPagamento MetodoPagamento { get; set; }
        public DateTime DataRicarica { get; set; } = DateTime.UtcNow;
        public string? TransactionId { get; set; }
        public StatoPagamento Stato { get; set; } = StatoPagamento.Completato;
        
        // Relazioni
        public int UtenteId { get; set; }
        public Utente Utente { get; set; } = null!;
        
        // Campi aggiuntivi
        public decimal SaldoPrecedente { get; set; }
        public decimal SaldoFinale { get; set; }
        public string? Note { get; set; }
    }
}