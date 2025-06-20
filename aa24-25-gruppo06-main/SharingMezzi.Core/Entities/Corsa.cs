namespace SharingMezzi.Core.Entities
{
    public enum StatoCorsa 
    { 
        InCorso, 
        Completata, 
        CompletataConDebito,  // NUOVO: quando l'utente termina con credito insufficiente
        Annullata 
    }
      public class Corsa : BaseEntity
    {
        public DateTime Inizio { get; set; }
        public DateTime? Fine { get; set; }
        public int DurataMinuti { get; set; } = 0; // Default a 0, non nullable
        public decimal CostoTotale { get; set; } = 0; // Default a 0, non nullable
        public StatoCorsa Stato { get; set; } = StatoCorsa.InCorso;
        
        // Relazioni
        public int UtenteId { get; set; }
        public Utente Utente { get; set; } = null!;
        
        public int MezzoId { get; set; }
        public Mezzo Mezzo { get; set; } = null!;
        
        public int ParcheggioPartenzaId { get; set; }
        public Parcheggio ParcheggioPartenza { get; set; } = null!;
        
        public int? ParcheggioDestinazioneId { get; set; }
        public Parcheggio? ParcheggioDestinazione { get; set; }
        
        public Pagamento? Pagamento { get; set; }
        
        // NUOVO: Tracking punti eco assegnati
        public int? PuntiEcoAssegnati { get; set; }
        
        // Helper methods
        public bool IsAttiva => Stato == StatoCorsa.InCorso;
        public bool IsCompletata => Stato == StatoCorsa.Completata || Stato == StatoCorsa.CompletataConDebito;
    }
}