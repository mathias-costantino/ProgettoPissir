namespace SharingMezzi.Core.Entities
{
    public enum RuoloUtente { Utente, Amministratore }
    public enum StatoUtente { Attivo, Sospeso, Bloccato }
    
    public class Utente : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cognome { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public RuoloUtente Ruolo { get; set; } = RuoloUtente.Utente;
        
        // NUOVO: Credito e punti eco
        public decimal Credito { get; set; } = 0;
        public int PuntiEco { get; set; } = 0;
        public decimal CreditoMinimo { get; set; } = 5.00m; // Credito minimo per iniziare una corsa
        
        // NUOVO: Stato utente (per gestire sospensioni)
        public StatoUtente Stato { get; set; } = StatoUtente.Attivo;
        public DateTime? DataSospensione { get; set; }
        public string? MotivoSospensione { get; set; }
        
        public bool IsAttivo => Stato == StatoUtente.Attivo;
        public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;
        
        // JWT refresh token support
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        
        // Relazioni
        public ICollection<Corsa> Corse { get; set; } = new List<Corsa>();
        public ICollection<Pagamento> Pagamenti { get; set; } = new List<Pagamento>();
        public ICollection<Ricarica> Ricariche { get; set; } = new List<Ricarica>();
        
        // Metodi helper
        public bool HaCreditoSufficiente(decimal importo)
        {
            return Credito >= importo && Stato == StatoUtente.Attivo;
        }
          public void AddebitaCredito(decimal importo)
        {
            Credito -= importo;
            // Solo utenti normali vengono sospesi per credito insufficiente
            // Gli amministratori possono avere credito negativo
            if (Credito < 0 && Ruolo != RuoloUtente.Amministratore)
            {
                Stato = StatoUtente.Sospeso;
                DataSospensione = DateTime.UtcNow;
                MotivoSospensione = "Credito insufficiente";
            }
        }
        
        public void RicaricaCredito(decimal importo)
        {
            Credito += importo;
            
            // Se l'utente era sospeso per credito insufficiente e ora ha credito sufficiente
            if (Stato == StatoUtente.Sospeso && 
                MotivoSospensione == "Credito insufficiente" && 
                Credito >= CreditoMinimo)
            {
                // Nota: la riattivazione completa richiede approvazione del gestore
                // Questo è solo per tracciare che ha ricaricato
                MotivoSospensione = "In attesa di riattivazione";
            }
        }
        
        public void AggiungiPuntiEco(int punti)
        {
            PuntiEco += punti;
        }
        
        public decimal ConvertiPuntiInSconto(int puntiDaConvertire)
        {
            if (puntiDaConvertire > PuntiEco) return 0;
            
            // 100 punti = 1€ di sconto
            decimal sconto = puntiDaConvertire / 100m;
            PuntiEco -= puntiDaConvertire;
            return sconto;
        }
    }
}