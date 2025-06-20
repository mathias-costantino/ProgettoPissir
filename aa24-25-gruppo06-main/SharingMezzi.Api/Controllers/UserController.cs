using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using SharingMezzi.Infrastructure.Database;
using System.Security.Claims;
// Add the following using if ConvertiPuntiDto is in another namespace
// using SharingMezzi.Core.DTOs; // Uncomment if not already present

namespace SharingMezzi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly SharingMezziContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(SharingMezziContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ottieni il profilo dell'utente corrente
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<UtenteDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var utente = await _context.Utenti.FindAsync(userId);

            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            var utenteDto = new UtenteDto
            {
                Id = utente.Id,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Email = utente.Email,
                Telefono = utente.Telefono,
                Ruolo = utente.Ruolo.ToString(),
                DataRegistrazione = utente.DataRegistrazione,
                // NUOVO
                Credito = utente.Credito,
                PuntiEco = utente.PuntiEco,
                Stato = utente.Stato.ToString(),
                DataSospensione = utente.DataSospensione,
                MotivoSospensione = utente.MotivoSospensione
            };

            return Ok(utenteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del profilo utente {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ricarica credito utente
    /// </summary>
    [HttpPost("ricarica-credito")]
    public async Task<ActionResult<RicaricaResponseDto>> RicaricaCredito([FromBody] RicaricaCreditoDto ricaricaDto)
    {
        try
        {
            if (ricaricaDto.Importo < 5 || ricaricaDto.Importo > 500)
            {
                return BadRequest(new { message = "Importo deve essere tra €5 e €500" });
            }

            var utente = await _context.Utenti.FindAsync(ricaricaDto.UtenteId);
            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            // Registra ricarica
            var ricarica = new Ricarica
            {
                UtenteId = utente.Id,
                Importo = ricaricaDto.Importo,
                MetodoPagamento = Enum.Parse<MetodoPagamento>(ricaricaDto.MetodoPagamento),
                DataRicarica = DateTime.UtcNow,
                TransactionId = Guid.NewGuid().ToString(),
                SaldoPrecedente = utente.Credito,
                SaldoFinale = utente.Credito + ricaricaDto.Importo,
                Stato = StatoPagamento.Completato
            };

            // Aggiorna credito utente
            utente.RicaricaCredito(ricaricaDto.Importo);
            
            // Aggiorna l'utente nel database
            _context.Utenti.Update(utente);
            await _context.Ricariche.AddAsync(ricarica);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ricarica credito completata: Utente {UserId}, Importo €{Importo}", 
                utente.Id, ricaricaDto.Importo);

            return Ok(new RicaricaResponseDto
            {
                Success = true,
                NuovoCredito = utente.Credito,
                Message = $"Ricarica di €{ricaricaDto.Importo:F2} completata con successo",
                TransactionId = ricarica.TransactionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricarica credito");
            return StatusCode(500, new { message = "Errore durante la ricarica" });
        }
    }

    /// <summary>
    /// Converti punti eco in credito
    /// </summary>
    [HttpPost("converti-punti")]
    public async Task<ActionResult> ConvertiPuntiEco([FromBody] ConvertiPuntiDto convertiDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var utente = await _context.Utenti.FindAsync(userId);

            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            if (convertiDto.PuntiDaConvertire > utente.PuntiEco)
            {
                return BadRequest(new { message = $"Punti insufficienti. Disponibili: {utente.PuntiEco}" });
            }

            if (convertiDto.PuntiDaConvertire < 100)
            {
                return BadRequest(new { message = "Minimo 100 punti per la conversione" });
            }

            decimal sconto = utente.ConvertiPuntiInSconto(convertiDto.PuntiDaConvertire);
            utente.Credito += sconto;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Conversione punti eco: Utente {UserId}, Punti {Punti}, Credito €{Credito}", 
                userId, convertiDto.PuntiDaConvertire, sconto);

            return Ok(new
            {
                success = true,
                puntiConvertiti = convertiDto.PuntiDaConvertire,
                creditoAggiunto = sconto,
                nuovoCredito = utente.Credito,
                puntiRimanenti = utente.PuntiEco
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la conversione punti eco");
            return StatusCode(500, new { message = "Errore durante la conversione" });
        }
    }

    /// <summary>
    /// Storico ricariche utente
    /// </summary>
    [HttpGet("{userId}/ricariche")]
    public async Task<ActionResult<IEnumerable<object>>> GetRicariche(int userId)
    {
        try
        {
            var ricariche = await _context.Ricariche
                .Where(r => r.UtenteId == userId)
                .OrderByDescending(r => r.DataRicarica)
                .Select(r => new
                {
                    r.Id,
                    r.Importo,
                    r.DataRicarica,
                    r.MetodoPagamento,
                    r.TransactionId,
                    r.SaldoPrecedente,
                    r.SaldoFinale
                })
                .ToListAsync();

            return Ok(ricariche);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero ricariche");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Statistiche utente
    /// </summary>
    [HttpGet("{userId}/statistiche")]
    public async Task<ActionResult<UserStatisticsDto>> GetStatistiche(int userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId && !User.IsInRole("Amministratore"))
            {
                return Forbid("Non autorizzato a visualizzare le statistiche di altri utenti");
            }

            var utente = await _context.Utenti.FindAsync(userId);
            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            var corse = await _context.Corse
                .Include(c => c.Mezzo)
                .Where(c => c.UtenteId == userId)
                .ToListAsync();

            var corseCompletate = corse.Where(c => c.Fine != null).ToList();
            var minutiTotali = corseCompletate.Sum(c => c.DurataMinuti);
            var spesaTotale = corseCompletate.Sum(c => c.CostoTotale);

            var mezzoPreferito = await _context.Corse
                .Include(c => c.Mezzo)
                .Where(c => c.UtenteId == userId && c.Fine != null)
                .GroupBy(c => c.Mezzo.Modello)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var stats = new UserStatisticsDto
            {
                TotaleCorse = corse.Count,
                CorseCompletate = corseCompletate.Count,
                CorseAnnullate = corse.Count(c => c.Fine == null && c.Inizio < DateTime.UtcNow.AddHours(-24)),
                MinutiTotali = minutiTotali,
                SpesaTotale = spesaTotale,
                CreditoAttuale = utente.Credito,
                PuntiEcoTotali = utente.PuntiEco,
                MezzoPreferito = mezzoPreferito,
                UltimaCorsa = corse.OrderByDescending(c => c.Inizio).FirstOrDefault()?.Inizio,
                DistanzaTotaleKm = 0 // Campo non presente nell'entità Corsa
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle statistiche");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Cronologia pagamenti e ricariche utente
    /// </summary>
    [HttpGet("{userId}/pagamenti")]
    public async Task<ActionResult<IEnumerable<object>>> GetPagamenti(int userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId && !User.IsInRole("Amministratore"))
            {
                return Forbid("Non autorizzato a visualizzare i pagamenti di altri utenti");
            }

            // Ottieni ricariche
            var ricariche = await _context.Ricariche
                .Where(r => r.UtenteId == userId)
                .Select(r => new
                {
                    tipo = "Ricarica",
                    importo = r.Importo,
                    data = r.DataRicarica,
                    metodoPagamento = r.MetodoPagamento.ToString(),
                    stato = r.Stato.ToString(),
                    transactionId = r.TransactionId
                })
                .ToListAsync();

            // Ottieni pagamenti
            var pagamenti = await _context.Pagamenti
                .Where(p => p.UtenteId == userId)
                .Select(p => new
                {
                    tipo = "Pagamento",
                    importo = p.Importo,
                    data = p.DataPagamento,
                    metodoPagamento = p.Metodo.ToString(),
                    stato = p.Stato.ToString(),
                    transactionId = p.TransactionId
                })
                .ToListAsync();

            // Combina e ordina per data
            var cronologia = ricariche.Cast<object>()
                .Concat(pagamenti.Cast<object>())
                .OrderByDescending(x => ((dynamic)x).data)
                .Take(50) // Limita a ultimi 50 movimenti
                .ToList();

            return Ok(cronologia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei pagamenti");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Aggiorna profilo utente
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<UtenteDto>> UpdateProfile([FromBody] UpdateProfileDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var utente = await _context.Utenti.FindAsync(userId);

            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            // Aggiorna solo i campi forniti
            if (!string.IsNullOrEmpty(updateDto.Nome))
                utente.Nome = updateDto.Nome;
            
            if (!string.IsNullOrEmpty(updateDto.Cognome))
                utente.Cognome = updateDto.Cognome;
            
            if (updateDto.Telefono != null)
                utente.Telefono = updateDto.Telefono;

            await _context.SaveChangesAsync();

            var utenteDto = new UtenteDto
            {
                Id = utente.Id,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Email = utente.Email,
                Telefono = utente.Telefono,
                Ruolo = utente.Ruolo.ToString(),
                DataRegistrazione = utente.DataRegistrazione,
                Credito = utente.Credito,
                PuntiEco = utente.PuntiEco,
                Stato = utente.Stato.ToString(),
                DataSospensione = utente.DataSospensione,
                MotivoSospensione = utente.MotivoSospensione
            };

            return Ok(utenteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del profilo");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Cambia password utente
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var utente = await _context.Utenti.FindAsync(userId);

            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            // Verifica password attuale
            var currentPasswordHash = HashPassword(changePasswordDto.PasswordAttuale);
            if (utente.Password != currentPasswordHash)
            {
                return BadRequest(new { message = "Password attuale non corretta" });
            }

            // Aggiorna password
            utente.Password = HashPassword(changePasswordDto.NuovaPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password cambiata per utente {UserId}", userId);
            return Ok(new { message = "Password cambiata con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il cambio password");
            return StatusCode(500, new { message = "Errore durante il cambio password" });
        }
    }

    /// <summary>
    /// Ottieni corse dell'utente corrente (per dashboard)
    /// </summary>
    [HttpGet("corse")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyCorse()
    {
        try
        {
            var userId = GetCurrentUserId();
            var corse = await _context.Corse
                .Include(c => c.Mezzo)
                .Include(c => c.ParcheggioPartenza)
                .Include(c => c.ParcheggioDestinazione)
                .Where(c => c.UtenteId == userId)
                .OrderByDescending(c => c.Inizio)
                .Take(10) // Ultime 10 corse per dashboard
                .Select(c => new
                {
                    c.Id,
                    c.Inizio,
                    c.Fine,
                    c.DurataMinuti,
                    c.CostoTotale,
                    c.Stato,
                    MezzoModello = c.Mezzo.Modello,
                    MezzoTipo = c.Mezzo.Tipo.ToString(),
                    ParcheggioPartenza = c.ParcheggioPartenza != null ? c.ParcheggioPartenza.Nome : "N/A",
                    ParcheggioDestinazione = c.ParcheggioDestinazione != null ? c.ParcheggioDestinazione.Nome : "N/A"
                })
                .ToListAsync();

            return Ok(corse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle corse utente");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Admin: Riattiva utente sospeso
    /// </summary>
    [HttpPost("{userId}/riattiva")]
    [Authorize(Roles = "Amministratore")]
    public async Task<ActionResult> RiattivaUtente(int userId)
    {
        try
        {
            var utente = await _context.Utenti.FindAsync(userId);
            if (utente == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            if (utente.Stato != StatoUtente.Sospeso)
            {
                return BadRequest(new { message = "L'utente non è sospeso" });
            }

            // Verifica che abbia credito sufficiente
            if (utente.Credito < utente.CreditoMinimo)
            {
                return BadRequest(new { 
                    message = $"L'utente deve avere almeno €{utente.CreditoMinimo:F2} di credito. Attuale: €{utente.Credito:F2}" 
                });
            }

            utente.Stato = StatoUtente.Attivo;
            utente.DataSospensione = null;
            utente.MotivoSospensione = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Utente {UserId} riattivato da admin {AdminId}", 
                userId, GetCurrentUserId());

            return Ok(new { message = "Utente riattivato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la riattivazione utente");
            return StatusCode(500, new { message = "Errore durante la riattivazione" });
        }
    }

    // Existing methods remain the same...
    
    private int GetCurrentUserId()
    {
        var userId = User.FindFirst("user_id")?.Value ?? 
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                    User.FindFirst("sub")?.Value ?? 
                    User.FindFirst("id")?.Value;
        return int.TryParse(userId, out var id) ? id : 0;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}