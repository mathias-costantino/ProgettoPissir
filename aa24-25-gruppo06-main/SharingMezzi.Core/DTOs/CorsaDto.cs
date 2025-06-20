namespace SharingMezzi.Core.DTOs
{
    public class CorsaDto
    {
        public int Id { get; set; }
        public DateTime Inizio { get; set; }
        public DateTime? Fine { get; set; }
        public int DurataMinuti { get; set; }
        public decimal CostoTotale { get; set; }
        public string Stato { get; set; } = string.Empty;
        public int UtenteId { get; set; }
        public int MezzoId { get; set; }
        public int ParcheggioPartenzaId { get; set; }
        public int? ParcheggioDestinazioneId { get; set; }
          // Proprietà aggiuntive per la visualizzazione
        public string? NomeMezzo { get; set; }
        public string? NomeParcheggioInizio { get; set; }        public string? NomeParcheggioFine { get; set; }
        
        // Proprietà del mezzo per il frontend
        public string? MezzoModello { get; set; }
        public string? MezzoTipo { get; set; }
        public decimal? TariffaPerMinuto { get; set; }
        public decimal? TariffaFissa { get; set; }
        public bool? IsElettrico { get; set; }
        
        // NUOVO: Punti eco assegnati durante la corsa
        public int? PuntiEcoAssegnati { get; set; }
        
        // Alias per compatibilità
        public DateTime DataInizio => Inizio;
        public DateTime? DataFine => Fine;
        public int ParcheggioInizioId => ParcheggioPartenzaId;
        public int? ParcheggioFineId => ParcheggioDestinazioneId;
        public decimal DistanzaPercorsa { get; set; } = 0; // Non presente nell'entità originale
    }    public class IniziaCorsa
    {
        public int UtenteId { get; set; }
        public int MezzoId { get; set; }
    }

    public class TerminaCorsa
    {
        public int ParcheggioDestinazioneId { get; set; }
        public bool SegnalaManutenzione { get; set; } = false;
        public string? DescrizioneManutenzione { get; set; }
    }
}