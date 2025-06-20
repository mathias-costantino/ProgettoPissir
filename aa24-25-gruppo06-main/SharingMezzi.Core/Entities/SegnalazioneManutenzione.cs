using System.ComponentModel.DataAnnotations;

namespace SharingMezzi.Core.Entities
{
    public enum StatoSegnalazione 
    { 
        Aperta, 
        InLavorazione, 
        Completata, 
        Respinta 
    }

    public enum PrioritaSegnalazione 
    { 
        Bassa, 
        Media, 
        Alta, 
        Critica 
    }

    public class SegnalazioneManutenzione : BaseEntity
    {
        [Required]
        public int MezzoId { get; set; }
        public Mezzo Mezzo { get; set; } = null!;

        [Required]
        public int UtenteId { get; set; }
        public Utente Utente { get; set; } = null!;

        public int? CorsaId { get; set; }
        public Corsa? Corsa { get; set; }

        [Required]
        public string Descrizione { get; set; } = string.Empty;

        public StatoSegnalazione Stato { get; set; } = StatoSegnalazione.Aperta;
        public PrioritaSegnalazione Priorita { get; set; } = PrioritaSegnalazione.Media;

        public DateTime DataSegnalazione { get; set; } = DateTime.UtcNow;
        public DateTime? DataRisoluzione { get; set; }

        public string? NoteRisoluzione { get; set; }
        public int? TecnicoId { get; set; }
        public Utente? Tecnico { get; set; }

        // Metodi helper
        public bool IsAperta => Stato == StatoSegnalazione.Aperta;
        public bool IsRisolta => Stato == StatoSegnalazione.Completata;
        public int GiorniAperti => DateTime.UtcNow.Subtract(DataSegnalazione).Days;
    }
}
