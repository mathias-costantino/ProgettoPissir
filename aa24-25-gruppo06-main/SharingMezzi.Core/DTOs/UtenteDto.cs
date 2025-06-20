namespace SharingMezzi.Core.DTOs;

public class UtenteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Ruolo { get; set; } = string.Empty;
    public DateTime DataRegistrazione { get; set; }
    
    // NUOVO: Credito e punti eco
    public decimal Credito { get; set; }
    public int PuntiEco { get; set; }
    public string Stato { get; set; } = "Attivo";
    public DateTime? DataSospensione { get; set; }
    public string? MotivoSospensione { get; set; }
}

public class UpdateProfileDto
{
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
}

public class RicaricaCreditoDto
{
    public int UtenteId { get; set; }
    public decimal Importo { get; set; }
    public string MetodoPagamento { get; set; } = "CartaCredito";
}

public class ConvertiPuntiDto
{
    public int PuntiDaConvertire { get; set; }
}

public class RicaricaResponseDto
{
    public bool Success { get; set; }
    public decimal NuovoCredito { get; set; }
    public string? Message { get; set; }
    public string? TransactionId { get; set; }
}

public class ChangePasswordDto
{
    public string PasswordAttuale { get; set; } = string.Empty;
    public string NuovaPassword { get; set; } = string.Empty;
}

public class UserStatisticsDto
{
    public int TotaleCorse { get; set; }
    public int CorseCompletate { get; set; }
    public int CorseAnnullate { get; set; }
    public int MinutiTotali { get; set; }
    public decimal SpesaTotale { get; set; }
    public decimal CreditoAttuale { get; set; }
    public int PuntiEcoTotali { get; set; }
    public TimeSpan TempoTotaleUtilizzo { get; set; }
    public string MezzoPreferito { get; set; } = string.Empty;
    public DateTime? UltimaCorsa { get; set; }
    public decimal DistanzaTotaleKm { get; set; }
}

public class AdminUserDto : UtenteDto
{
    public int NumeroCorseEffettuate { get; set; }
    public decimal SpesaTotale { get; set; }
    public DateTime? UltimaAttivita { get; set; }
    public int NumeroSospensioni { get; set; }
    public bool PuoEssereRiattivato { get; set; }
}

public class RicaricaDto
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public decimal Importo { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty;
    public DateTime DataRicarica { get; set; }
    public string? TransactionId { get; set; }
    public string Stato { get; set; } = string.Empty;
    public decimal SaldoPrecedente { get; set; }
    public decimal SaldoFinale { get; set; }
    public string? Note { get; set; }
}

public class AdminRiparazioneMezzoDto
{
    public int MezzoId { get; set; }
    public string NoteRiparazione { get; set; } = string.Empty;
}

public class AdminSbloccaUtenteDto
{
    public int UtenteId { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class AdminSospendUtenteDto
{
    public int UtenteId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}