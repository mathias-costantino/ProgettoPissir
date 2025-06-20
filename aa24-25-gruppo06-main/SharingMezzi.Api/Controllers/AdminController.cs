using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;
using SharingMezzi.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using SharingMezzi.Core.Entities;
using SharingMezzi.IoT.Services;

namespace SharingMezzi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Solo utenti autenticati
    public class AdminController : ControllerBase
    {
        private readonly SharingMezziContext _context;
        private readonly IMezzoService _mezzoService;
        private readonly IParcheggioService _parcheggioService;
        private readonly ConnectedIoTClientsService _iotClientsService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            SharingMezziContext context,
            IMezzoService mezzoService,
            IParcheggioService parcheggioService,
            ConnectedIoTClientsService iotClientsService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _mezzoService = mezzoService;
            _parcheggioService = parcheggioService;
            _iotClientsService = iotClientsService;
            _logger = logger;
        }

        /// <summary>
        /// Ottieni statistiche dashboard admin
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<AdminStatisticsDto>> GetStatistics()
        {
            try
            {
                var mezzi = await _context.Mezzi.ToListAsync();
                var parcheggi = await _context.Parcheggi.ToListAsync();
                var corseAttive = await _context.Corse
                    .Where(c => c.Stato == StatoCorsa.InCorso)
                    .CountAsync();

                var stats = new AdminStatisticsDto
                {
                    TotalMezzi = mezzi.Count,
                    MezziDisponibili = mezzi.Count(m => m.Stato == StatoMezzo.Disponibile),
                    MezziInUso = mezzi.Count(m => m.Stato == StatoMezzo.Occupato),
                    MezziManutenzione = mezzi.Count(m => m.Stato == StatoMezzo.Manutenzione),
                    TotalParcheggi = parcheggi.Count,
                    BatteriaBassa = mezzi.Count(m => m.LivelloBatteria < 20),
                    CorseAttive = corseAttive,
                    UltimoAggiornamento = DateTime.UtcNow
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero delle statistiche admin");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottieni mezzi che necessitano manutenzione
        /// </summary>
        [HttpGet("maintenance-alerts")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<IEnumerable<MaintenanceAlertDto>>> GetMaintenanceAlerts()
        {
            try
            {
                var alerts = new List<MaintenanceAlertDto>();

                // Mezzi con batteria bassa
                var lowBatteryMezzi = await _context.Mezzi
                    .Where(m => m.LivelloBatteria < 20)
                    .ToListAsync();

                foreach (var mezzo in lowBatteryMezzi)
                {
                    alerts.Add(new MaintenanceAlertDto
                    {
                        MezzoId = mezzo.Id,
                        Type = "battery",
                        Priority = mezzo.LivelloBatteria < 10 ? "critical" : "high",
                        Message = $"Batteria al {mezzo.LivelloBatteria}%",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Mezzi in manutenzione
                var maintenanceMezzi = await _context.Mezzi
                    .Where(m => m.Stato == StatoMezzo.Manutenzione)
                    .ToListAsync();

                foreach (var mezzo in maintenanceMezzi)
                {
                    alerts.Add(new MaintenanceAlertDto
                    {
                        MezzoId = mezzo.Id,
                        Type = "maintenance",
                        Priority = "medium",
                        Message = "Mezzo in manutenzione",
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Ok(alerts.OrderByDescending(a => a.Priority == "critical" ? 3 : 
                                                      a.Priority == "high" ? 2 : 1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero degli alert di manutenzione");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Forza aggiornamento di tutti i parcheggi
        /// </summary>
        [HttpPost("refresh-all")]
        [Authorize(Roles = "Amministratore")]
        public async Task<IActionResult> RefreshAllParkings()
        {
            try
            {
                var parcheggi = await _context.Parcheggi.ToListAsync();
                
                foreach (var parcheggio in parcheggi)
                {
                    await _parcheggioService.UpdatePostiLiberiAsync(parcheggio.Id);
                }

                _logger.LogInformation("Aggiornamento forzato di tutti i parcheggi completato");
                return Ok(new { message = "Tutti i parcheggi sono stati aggiornati" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento forzato dei parcheggi");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottieni stato sistema IoT/MQTT
        /// </summary>
        [HttpGet("system-status")]
        [Authorize(Roles = "Amministratore")]
        public ActionResult<SystemStatusDto> GetSystemStatus()
        {
            try
            {
                // Ottieni statistiche reali dei dispositivi IoT
                var iotStats = _iotClientsService.GetConnectionStats();
                dynamic stats = iotStats;
                var connectedDevices = (int)stats.ConnectedClients;
                
                var status = new SystemStatusDto
                {
                    MqttBrokerStatus = "online", // Controlla il tuo SharingMezziBroker
                    IoTDevicesConnected = connectedDevices, // Ora usa i dati reali!
                    SignalRStatus = "active",     // Controlla SignalR hubs
                    DatabaseStatus = "healthy",   // Controlla connessione DB
                    LastUpdate = DateTime.UtcNow
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dello stato sistema");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Programma manutenzione per un mezzo
        /// </summary>
        [HttpPost("schedule-maintenance/{mezzoId}")]
        [Authorize(Roles = "Amministratore")]
        public async Task<IActionResult> ScheduleMaintenance(int mezzoId, [FromBody] ScheduleMaintenanceDto request)
        {
            try
            {
                var mezzo = await _context.Mezzi.FindAsync(mezzoId);
                if (mezzo == null)
                {
                    return NotFound($"Mezzo {mezzoId} non trovato");
                }

                mezzo.Stato = StatoMezzo.Manutenzione;
                mezzo.UltimaManutenzione = DateTime.UtcNow;

                // Crea record di manutenzione (se hai una tabella per questo)
                var segnalazione = new SegnalazioneManutenzione
                {
                    MezzoId = mezzoId,
                    Descrizione = request.Note ?? "Manutenzione programmata da admin",
                    DataSegnalazione = DateTime.UtcNow,
                    Stato = StatoSegnalazione.Aperta
                };

                _context.SegnalazioniManutenzione.Add(segnalazione);
                await _context.SaveChangesAsync();

                // Aggiorna contatori parcheggio se il mezzo ha un parcheggio assegnato
                if (mezzo.ParcheggioId.HasValue)
                {
                    await _parcheggioService.UpdatePostiLiberiAsync(mezzo.ParcheggioId.Value);
                }

                _logger.LogInformation("Manutenzione programmata per mezzo {MezzoId}", mezzoId);
                
                return Ok(new { message = $"Manutenzione programmata per mezzo {mezzoId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella programmazione manutenzione per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottieni tutti gli utenti (solo admin)
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<IEnumerable<UtenteDto>>> GetAllUsers()
        {
            try
            {
                var utenti = await _context.Utenti
                    .Select(u => new UtenteDto
                    {
                        Id = u.Id,
                        Nome = u.Nome,
                        Cognome = u.Cognome,
                        Email = u.Email,
                        Telefono = u.Telefono,
                        Ruolo = u.Ruolo.ToString(),
                        DataRegistrazione = u.DataRegistrazione,
                        Credito = u.Credito,
                        PuntiEco = u.PuntiEco,
                        Stato = u.Stato.ToString(),
                        DataSospensione = u.DataSospensione,
                        MotivoSospensione = u.MotivoSospensione
                    })
                    .ToListAsync();

                return Ok(utenti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero utenti");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottieni utenti sospesi (solo admin)
        /// </summary>
        [HttpGet("users/suspended")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<IEnumerable<UtenteDto>>> GetSuspendedUsers()
        {
            try
            {
                var utentiSospesi = await _context.Utenti
                    .Where(u => u.Stato == StatoUtente.Sospeso)
                    .Select(u => new UtenteDto
                    {
                        Id = u.Id,
                        Nome = u.Nome,
                        Cognome = u.Cognome,
                        Email = u.Email,
                        Stato = u.Stato.ToString(),
                        DataSospensione = u.DataSospensione,
                        MotivoSospensione = u.MotivoSospensione,
                        Credito = u.Credito
                    })
                    .ToListAsync();

                return Ok(utentiSospesi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero utenti sospesi");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Sblocca utente sospeso (solo admin)
        /// </summary>
        [HttpPost("users/{userId}/unblock")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult> UnblockUser(int userId, [FromBody] AdminSbloccaUtenteDto request)
        {
            try
            {
                var utente = await _context.Utenti.FindAsync(userId);
                if (utente == null)
                {
                    return NotFound("Utente non trovato");
                }

                if (utente.Stato != StatoUtente.Sospeso)
                {
                    return BadRequest("L'utente non è sospeso");
                }

                utente.Stato = StatoUtente.Attivo;
                utente.DataSospensione = null;
                utente.MotivoSospensione = null;
                utente.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Utente {UserId} sbloccato da admin", userId);
                return Ok(new { message = $"Utente {utente.Nome} {utente.Cognome} sbloccato con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nello sblocco utente {UserId}", userId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Sospendi utente (solo admin)
        /// </summary>
        [HttpPost("users/{userId}/suspend")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult> SuspendUser(int userId, [FromBody] AdminSospendUtenteDto request)
        {
            try
            {
                var utente = await _context.Utenti.FindAsync(userId);
                if (utente == null)
                {
                    return NotFound("Utente non trovato");
                }

                if (utente.Ruolo == RuoloUtente.Amministratore)
                {
                    return BadRequest("Non è possibile sospendere un amministratore");
                }

                utente.Stato = StatoUtente.Sospeso;
                utente.DataSospensione = DateTime.UtcNow;
                utente.MotivoSospensione = request.Motivo;
                utente.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Utente {UserId} sospeso da admin per: {Motivo}", userId, request.Motivo);
                return Ok(new { message = $"Utente {utente.Nome} {utente.Cognome} sospeso con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella sospensione utente {UserId}", userId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ripara mezzo in manutenzione (solo admin)
        /// </summary>
        [HttpPost("vehicles/{mezzoId}/repair")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult> RepairVehicle(int mezzoId, [FromBody] AdminRiparazioneMezzoDto request)
        {
            try
            {
                var mezzo = await _context.Mezzi.FindAsync(mezzoId);
                if (mezzo == null)
                {
                    return NotFound("Mezzo non trovato");
                }

                if (mezzo.Stato != StatoMezzo.Manutenzione && mezzo.Stato != StatoMezzo.Guasto)
                {
                    return BadRequest("Il mezzo non è in manutenzione o guasto");
                }

                // Aggiorna stato mezzo
                mezzo.Stato = StatoMezzo.Disponibile;
                mezzo.UltimaManutenzione = DateTime.UtcNow;
                mezzo.UpdatedAt = DateTime.UtcNow;

                // Chiudi segnalazioni aperte per questo mezzo
                var segnalazioniAperte = await _context.SegnalazioniManutenzione
                    .Where(s => s.MezzoId == mezzoId && s.Stato == StatoSegnalazione.Aperta)
                    .ToListAsync();

                foreach (var segnalazione in segnalazioniAperte)
                {
                    segnalazione.Stato = StatoSegnalazione.Completata;
                    segnalazione.DataRisoluzione = DateTime.UtcNow;
                    segnalazione.NoteRisoluzione = request.NoteRiparazione;
                    segnalazione.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Aggiorna contatori parcheggio se il mezzo ha un parcheggio assegnato
                if (mezzo.ParcheggioId.HasValue)
                {
                    await _parcheggioService.UpdatePostiLiberiAsync(mezzo.ParcheggioId.Value);
                }

                _logger.LogInformation("Mezzo {MezzoId} riparato da admin", mezzoId);
                return Ok(new { message = $"Mezzo {mezzo.Modello} riparato con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella riparazione mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Crea nuovo parcheggio (solo admin)
        /// </summary>
        [HttpPost("parking")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<ParcheggioDto>> CreateParking([FromBody] CreateParcheggioDto createDto)
        {
            try
            {
                var parcheggio = new Parcheggio
                {
                    Nome = createDto.Nome,
                    Indirizzo = createDto.Indirizzo,
                    Capienza = createDto.Capienza,
                    PostiLiberi = createDto.Capienza,
                    PostiOccupati = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Parcheggi.Add(parcheggio);
                await _context.SaveChangesAsync();

                var parcheggioDto = new ParcheggioDto
                {
                    Id = parcheggio.Id,
                    Nome = parcheggio.Nome,
                    Indirizzo = parcheggio.Indirizzo,
                    Capienza = parcheggio.Capienza,
                    PostiLiberi = parcheggio.PostiLiberi,
                    PostiOccupati = parcheggio.PostiOccupati
                };

                _logger.LogInformation("Nuovo parcheggio creato: {Nome}", parcheggio.Nome);
                return CreatedAtAction(nameof(CreateParking), new { id = parcheggio.Id }, parcheggioDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella creazione parcheggio");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Crea nuovo mezzo (solo admin)
        /// </summary>
        [HttpPost("vehicles")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<MezzoDto>> CreateVehicle([FromBody] CreateMezzoDto createDto)
        {
            try
            {
                var mezzo = new Mezzo
                {
                    Modello = createDto.Modello,
                    Tipo = Enum.Parse<TipoMezzo>(createDto.Tipo),
                    IsElettrico = createDto.IsElettrico,
                    Stato = StatoMezzo.Disponibile,
                    LivelloBatteria = createDto.IsElettrico ? 100 : null,
                    TariffaPerMinuto = createDto.TariffaPerMinuto,
                    TariffaFissa = createDto.TariffaFissa,
                    ParcheggioId = createDto.ParcheggioId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Mezzi.Add(mezzo);
                await _context.SaveChangesAsync();

                var mezzoDto = new MezzoDto
                {
                    Id = mezzo.Id,
                    Modello = mezzo.Modello,
                    Tipo = mezzo.Tipo.ToString(),
                    IsElettrico = mezzo.IsElettrico,
                    Stato = mezzo.Stato.ToString(),
                    LivelloBatteria = mezzo.LivelloBatteria,
                    TariffaPerMinuto = mezzo.TariffaPerMinuto,
                    TariffaFissa = mezzo.TariffaFissa,
                    ParcheggioId = mezzo.ParcheggioId
                };

                _logger.LogInformation("Nuovo mezzo creato: {Modello}", mezzo.Modello);
                return CreatedAtAction(nameof(CreateVehicle), new { id = mezzo.Id }, mezzoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella creazione mezzo");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Metti mezzo in manutenzione (solo admin)
        /// </summary>
        [HttpPost("vehicles/{mezzoId}/maintenance")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult> SetVehicleMaintenance(int mezzoId, [FromBody] AdminManutenzioneDto request)
        {
            try
            {
                var mezzo = await _context.Mezzi.FindAsync(mezzoId);
                if (mezzo == null)
                {
                    return NotFound("Mezzo non trovato");
                }

                if (mezzo.Stato == StatoMezzo.Occupato)
                {
                    return BadRequest("Il mezzo è attualmente in uso e non può essere messo in manutenzione");
                }

                // Aggiorna stato mezzo
                mezzo.Stato = StatoMezzo.Manutenzione;
                mezzo.UpdatedAt = DateTime.UtcNow;

                // Crea segnalazione di manutenzione
                var segnalazione = new SegnalazioneManutenzione
                {
                    MezzoId = mezzoId,
                    Descrizione = request.Note ?? "Manutenzione programmata da amministratore",
                    DataSegnalazione = DateTime.UtcNow,
                    Stato = StatoSegnalazione.Aperta,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SegnalazioniManutenzione.Add(segnalazione);
                await _context.SaveChangesAsync();

                // Aggiorna contatori parcheggio se il mezzo ha un parcheggio assegnato
                if (mezzo.ParcheggioId.HasValue)
                {
                    await _parcheggioService.UpdatePostiLiberiAsync(mezzo.ParcheggioId.Value);
                }

                _logger.LogInformation("Mezzo {MezzoId} messo in manutenzione da admin", mezzoId);
                return Ok(new { message = $"Mezzo {mezzo.Modello} messo in manutenzione con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel mettere mezzo {MezzoId} in manutenzione", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottieni tutti i mezzi in manutenzione (solo admin)
        /// </summary>
        [HttpGet("vehicles/maintenance")]
        [Authorize(Roles = "Amministratore")]
        public async Task<ActionResult<IEnumerable<MezzoMaintenanceDto>>> GetVehiclesInMaintenance()
        {
            try
            {
                var mezziManutenzione = await _context.Mezzi
                    .Where(m => m.Stato == StatoMezzo.Manutenzione || m.Stato == StatoMezzo.Guasto)
                    .Include(m => m.Parcheggio)
                    .Select(m => new MezzoMaintenanceDto
                    {
                        Id = m.Id,
                        Modello = m.Modello,
                        Tipo = m.Tipo.ToString(),
                        Stato = m.Stato.ToString(),
                        ParcheggioNome = m.Parcheggio != null ? m.Parcheggio.Nome : "Non assegnato",
                        UltimaManutenzione = m.UltimaManutenzione,
                        LivelloBatteria = m.LivelloBatteria
                    })
                    .ToListAsync();

                return Ok(mezziManutenzione);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero mezzi in manutenzione");
                return StatusCode(500, "Errore interno del server");
            }
        }

    }

    // DTO specifici per Admin
    public class AdminStatisticsDto
    {
        public int TotalMezzi { get; set; }
        public int MezziDisponibili { get; set; }
        public int MezziInUso { get; set; }
        public int MezziManutenzione { get; set; }
        public int TotalParcheggi { get; set; }
        public int BatteriaBassa { get; set; }
        public int CorseAttive { get; set; }
        public DateTime UltimoAggiornamento { get; set; }
    }

    public class MaintenanceAlertDto
    {
        public int MezzoId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class SystemStatusDto
    {
        public string MqttBrokerStatus { get; set; } = string.Empty;
        public int IoTDevicesConnected { get; set; }
        public string SignalRStatus { get; set; } = string.Empty;
        public string DatabaseStatus { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }
    }

    public class ScheduleMaintenanceDto
    {
        public string? Note { get; set; }
        public DateTime? ScheduledDate { get; set; }
    }

    public class AdminSbloccaUtenteDto
    {
        public string Note { get; set; } = string.Empty;
    }

    public class AdminSospendUtenteDto
    {
        public string Motivo { get; set; } = string.Empty;
    }

    public class AdminRiparazioneMezzoDto
    {
        public string NoteRiparazione { get; set; } = string.Empty;
    }

    public class AdminManutenzioneDto
    {
        public string Note { get; set; } = string.Empty;
    }

    public class MezzoMaintenanceDto
    {
        public int Id { get; set; }
        public string Modello { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Stato { get; set; } = string.Empty;
        public string ParcheggioNome { get; set; } = string.Empty;
        public DateTime? UltimaManutenzione { get; set; }
        public int? LivelloBatteria { get; set; }
    }
}