using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace SharingMezzi.Core.Services
{
    /// <summary>
    /// Interfaccia per il broker MQTT - permette l'invio di comandi agli attuatori
    /// e richieste di controllo batteria per mezzi elettrici
    /// </summary>
    public interface IMqttActuatorService
    {
        Task SendUnlockCommand(int mezzoId, int? corsaId = null);
        Task SendLockCommand(int mezzoId, int? corsaId = null);
        
        // NUOVO: Controllo batteria per mezzi elettrici
        Task<(bool IsAcceptable, int? BatteryLevel)> RequestBatteryCheck(int mezzoId, int timeoutSeconds = 10);
    }

    public class CorsaService : ICorsaService
    {
        private readonly IRepository<Corsa> _corsaRepository;
        private readonly IMezzoRepository _mezzoRepository;
        private readonly IRepository<Utente> _utenteRepository;
        private readonly IRepository<Parcheggio> _parcheggioRepository;
        private readonly IRepository<SegnalazioneManutenzione> _segnalazioneRepository;
        private readonly ILogger<CorsaService> _logger;
        private readonly IMqttActuatorService? _mqttActuatorService;
        private readonly IParcheggioService _parcheggioService;

        public CorsaService(
            IRepository<Corsa> corsaRepository,
            IMezzoRepository mezzoRepository,
            IRepository<Utente> utenteRepository,
            IRepository<Parcheggio> parcheggioRepository,
            IRepository<SegnalazioneManutenzione> segnalazioneRepository,
            ILogger<CorsaService> logger,
            IMqttActuatorService? mqttActuatorService,
            IParcheggioService parcheggioService)
        {
            _corsaRepository = corsaRepository;
            _mezzoRepository = mezzoRepository;
            _utenteRepository = utenteRepository;
            _parcheggioRepository = parcheggioRepository;
            _segnalazioneRepository = segnalazioneRepository;
            _logger = logger;
            _mqttActuatorService = mqttActuatorService;
            _parcheggioService = parcheggioService;
        }

        public async Task<CorsaDto> IniziaCorsa(IniziaCorsa comando)
        {
            // Verifica mezzo disponibile
            var mezzo = await _mezzoRepository.GetByIdAsync(comando.MezzoId);
            if (mezzo == null || mezzo.Stato != StatoMezzo.Disponibile)
                throw new InvalidOperationException("Mezzo non disponibile");

            // Verifica utente e credito
            var utente = await _utenteRepository.GetByIdAsync(comando.UtenteId);
            if (utente == null)
                throw new InvalidOperationException("Utente non trovato");
                
            if (utente.Stato != StatoUtente.Attivo)
                throw new InvalidOperationException($"Utente {utente.Stato}. Contattare l'assistenza.");

            // Verifica credito minimo (tariffa fissa + almeno 10 minuti di utilizzo)
            decimal costoMinimo = mezzo.TariffaFissa + (10 * mezzo.TariffaPerMinuto);
            if (!utente.HaCreditoSufficiente(costoMinimo))
            {
                throw new InvalidOperationException(
                    $"Credito insufficiente. Richiesti almeno €{costoMinimo:F2} " +
                    $"(tariffa fissa €{mezzo.TariffaFissa:F2} + costo per 10 minuti €{10 * mezzo.TariffaPerMinuto:F2}), " +
                    $"disponibili €{utente.Credito:F2}");
            }

            // Verifica se utente ha già una corsa attiva
            var corseUtente = await _corsaRepository.GetAllAsync();
            var corsaAttiva = corseUtente.FirstOrDefault(c => 
                c.UtenteId == comando.UtenteId && c.Stato == StatoCorsa.InCorso);
            
            if (corsaAttiva != null)
                throw new InvalidOperationException("Hai già una corsa in corso");

            // NUOVO: Controllo batteria per mezzi elettrici
            var (batteryAcceptable, batteryLevel) = await CheckBatteryLevel(comando.MezzoId);
            if (!batteryAcceptable)
            {
                throw new InvalidOperationException(
                    $"Livello batteria insufficiente ({batteryLevel}%). " +
                    "È richiesto almeno il 20% per iniziare una corsa.");
            }

            // Crea corsa
            var corsa = new Corsa
            {
                UtenteId = comando.UtenteId,
                MezzoId = comando.MezzoId,
                ParcheggioPartenzaId = mezzo.ParcheggioId ?? 0,
                Inizio = DateTime.UtcNow,
                Stato = StatoCorsa.InCorso,
                DurataMinuti = 0,
                CostoTotale = 0
            };

            corsa = await _corsaRepository.AddAsync(corsa);
            await _mezzoRepository.UpdateStatusAsync(comando.MezzoId, StatoMezzo.Occupato);

            // NUOVO: Aggiorna automaticamente il parcheggio di partenza
            if (mezzo.ParcheggioId.HasValue)
            {
                await _parcheggioService.UpdatePostiLiberiAsync(mezzo.ParcheggioId.Value);
                _logger.LogDebug("Aggiornato parcheggio partenza {ParcheggioId} dopo inizio corsa {CorsaId}", 
                    mezzo.ParcheggioId.Value, corsa.Id);
            }

            try
            {
                if (_mqttActuatorService != null)
                {
                    await _mqttActuatorService.SendUnlockCommand(comando.MezzoId, corsa.Id);
                    _logger.LogInformation("Sent unlock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                        comando.MezzoId, corsa.Id);
                }
                else
                {
                    _logger.LogWarning("MQTT Actuator Service not available - cannot send unlock command for Mezzo {MezzoId}", 
                        comando.MezzoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send unlock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                    comando.MezzoId, corsa.Id);
            }

            _logger.LogInformation("Started ride {CorsaId} for user {UserId} with vehicle {MezzoId}", 
                corsa.Id, comando.UtenteId, comando.MezzoId);

            return await MapToDtoWithDetails(corsa);
        }

        public async Task<CorsaDto> TerminaCorsa(int corsaId, TerminaCorsa comando)
        {
            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null || corsa.Stato != StatoCorsa.InCorso)
                throw new InvalidOperationException("Corsa non trovata o già terminata");

            var utente = await _utenteRepository.GetByIdAsync(corsa.UtenteId);
            if (utente == null)
                throw new InvalidOperationException("Utente non trovato");

            var mezzo = await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            if (mezzo == null)
                throw new InvalidOperationException("Mezzo non trovato");

            // Verifica che il parcheggio di destinazione esista
            var parcheggioDestinazione = await _parcheggioRepository.GetByIdAsync(comando.ParcheggioDestinazioneId);
            if (parcheggioDestinazione == null)
                throw new InvalidOperationException($"Parcheggio di destinazione con ID {comando.ParcheggioDestinazioneId} non trovato");

            // Calcola durata e costo
            corsa.Fine = DateTime.UtcNow;
            corsa.DurataMinuti = (int)(corsa.Fine.Value - corsa.Inizio).TotalMinutes;
            corsa.ParcheggioDestinazioneId = comando.ParcheggioDestinazioneId;
            corsa.CostoTotale = await CalcolaCosto(corsaId);

            // Addebita credito
            utente.AddebitaCredito(corsa.CostoTotale);
            
            // Assegna punti eco per bici muscolari
            if (mezzo.Tipo == TipoMezzo.BiciMuscolare)
            {
                int puntiEco = CalcolaPuntiEco(corsa.DurataMinuti);
                utente.AggiungiPuntiEco(puntiEco);
                corsa.PuntiEcoAssegnati = puntiEco;
                _logger.LogInformation("Assegnati {Punti} punti eco a utente {UserId}", 
                    puntiEco, utente.Id);
            }
            else
            {
                corsa.PuntiEcoAssegnati = 0;
            }

            // Aggiorna entità
            await _utenteRepository.UpdateAsync(utente);
            corsa.Stato = utente.Credito >= 0 ? StatoCorsa.Completata : StatoCorsa.CompletataConDebito;
            await _corsaRepository.UpdateAsync(corsa);
            
            // Gestione segnalazione manutenzione
            StatoMezzo nuovoStatoMezzo = StatoMezzo.Disponibile;
            
            if (comando.SegnalaManutenzione)
            {
                await CreaSegnalazioneManutenzione(corsa, comando.DescrizioneManutenzione);
                nuovoStatoMezzo = StatoMezzo.Manutenzione;
                _logger.LogWarning("Mezzo {MezzoId} segnalato per manutenzione dall'utente {UserId} durante corsa {CorsaId}", 
                    corsa.MezzoId, corsa.UtenteId, corsaId);
            }
            
            await _mezzoRepository.UpdateStatusAsync(corsa.MezzoId, nuovoStatoMezzo);
            
            // NUOVO: Aggiorna il parcheggio di destinazione dopo aver posizionato il mezzo
            var mezzoAggiornato = await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            if (mezzoAggiornato != null)
            {
                mezzoAggiornato.ParcheggioId = comando.ParcheggioDestinazioneId;
                await _mezzoRepository.UpdateAsync(mezzoAggiornato);
            }
            await _parcheggioService.UpdatePostiLiberiAsync(comando.ParcheggioDestinazioneId);
            _logger.LogDebug("Aggiornato parcheggio destinazione {ParcheggioId} dopo fine corsa {CorsaId}", 
                comando.ParcheggioDestinazioneId, corsaId);

            try
            {
                if (_mqttActuatorService != null)
                {
                    await _mqttActuatorService.SendLockCommand(corsa.MezzoId, corsaId);
                    _logger.LogInformation("Sent lock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                        corsa.MezzoId, corsaId);
                }
                else
                {
                    _logger.LogWarning("MQTT Actuator Service not available - cannot send lock command for Mezzo {MezzoId}", 
                        corsa.MezzoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send lock command for Mezzo {MezzoId}, Corsa {CorsaId}", 
                    corsa.MezzoId, corsaId);
            }

            _logger.LogInformation("Ended ride {CorsaId}, duration: {Duration} min, cost: €{Cost}, maintenance: {Maintenance}", 
                corsaId, corsa.DurataMinuti, corsa.CostoTotale, comando.SegnalaManutenzione);

            return await MapToDtoWithDetails(corsa);
        }

        public async Task<decimal> CalcolaCosto(int corsaId)
        {
            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null) return 0;

            var mezzo = await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            if (mezzo == null) return 0;

            decimal costoFisso = mezzo.TariffaFissa;
            decimal costoVariabile = corsa.DurataMinuti * mezzo.TariffaPerMinuto;
            
            return costoFisso + costoVariabile;
        }

        private int CalcolaPuntiEco(int durataMinuti)
        {
            int punti = durataMinuti;
            int bonus = (durataMinuti / 30) * 10;
            return punti + bonus;
        }

        public async Task<IEnumerable<CorsaDto>> GetCorseUtente(int utenteId)
        {
            var corse = await _corsaRepository.GetAllAsync();
            var corseUtente = corse.Where(c => c.UtenteId == utenteId).ToList();
            
            var result = new List<CorsaDto>();
            foreach (var corsa in corseUtente)
            {
                result.Add(await MapToDtoWithDetails(corsa));
            }
            
            return result;
        }

        public async Task<CorsaDto?> GetCorsaAttiva(int utenteId)
        {
            var corse = await _corsaRepository.GetAllAsync();
            var corsaAttiva = corse.FirstOrDefault(c => c.UtenteId == utenteId && c.Stato == StatoCorsa.InCorso);
            return corsaAttiva != null ? await MapToDtoWithDetails(corsaAttiva) : null;
        }

        /// <summary>
        /// Controlla il livello di batteria per mezzi elettrici tramite MQTT
        /// </summary>
        public async Task<(bool IsAcceptable, int? BatteryLevel)> CheckBatteryLevel(int mezzoId)
        {
            try
            {
                // Verifica se il mezzo esiste ed è elettrico
                var mezzo = await _mezzoRepository.GetByIdAsync(mezzoId);
                if (mezzo == null)
                {
                    _logger.LogWarning("Mezzo {MezzoId} non trovato per controllo batteria", mezzoId);
                    return (false, null);
                }

                // Se non è un mezzo elettrico, non serve controllare la batteria
                if (!mezzo.IsElettrico)
                {
                    _logger.LogDebug("Mezzo {MezzoId} non è elettrico, nessun controllo batteria necessario", mezzoId);
                    return (true, null);
                }

                // Per mezzi elettrici, richiede controllo via MQTT
                if (_mqttActuatorService != null)
                {
                    _logger.LogInformation("Richiesta controllo batteria MQTT per mezzo elettrico {MezzoId}", mezzoId);
                    var batteryResult = await _mqttActuatorService.RequestBatteryCheck(mezzoId, 10);
                    
                    if (batteryResult.BatteryLevel.HasValue)
                    {
                        // Soglia minima: 20% per permettere l'avvio della corsa
                        bool isAcceptable = batteryResult.BatteryLevel.Value >= 20;
                        _logger.LogInformation("Controllo batteria mezzo {MezzoId}: {Level}% - {Status}", 
                            mezzoId, batteryResult.BatteryLevel.Value, isAcceptable ? "Accettabile" : "Insufficiente");
                        
                        return (isAcceptable, batteryResult.BatteryLevel.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Controllo batteria MQTT fallito per mezzo {MezzoId} - dispositivo non risponde", mezzoId);
                        return (false, null);
                    }
                }
                else
                {
                    _logger.LogWarning("MQTT Actuator Service non disponibile - impossibile controllare batteria mezzo {MezzoId}", mezzoId);
                    // In caso di errore MQTT, permettiamo l'avvio (fallback)
                    return (true, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante controllo batteria mezzo {MezzoId}", mezzoId);
                // In caso di errore, permettiamo l'avvio (comportamento sicuro)
                return (true, null);
            }
        }

        private CorsaDto MapToDto(Corsa corsa)
        {
            return new CorsaDto
            {
                Id = corsa.Id,
                UtenteId = corsa.UtenteId,
                MezzoId = corsa.MezzoId,
                ParcheggioPartenzaId = corsa.ParcheggioPartenzaId,
                ParcheggioDestinazioneId = corsa.ParcheggioDestinazioneId,
                Inizio = corsa.Inizio,
                Fine = corsa.Fine,
                DurataMinuti = corsa.DurataMinuti,
                CostoTotale = corsa.CostoTotale,
                Stato = corsa.Stato.ToString(),
                MezzoModello = corsa.Mezzo?.Modello ?? "N/A",
                MezzoTipo = corsa.Mezzo?.Tipo.ToString() ?? "N/A",
                TariffaPerMinuto = corsa.Mezzo?.TariffaPerMinuto ?? 0,
                TariffaFissa = corsa.Mezzo?.TariffaFissa ?? 1.00m,
                IsElettrico = corsa.Mezzo?.IsElettrico ?? false,
                PuntiEcoAssegnati = corsa.PuntiEcoAssegnati,
                NomeParcheggioInizio = corsa.ParcheggioPartenza?.Nome ?? "N/A",
                NomeParcheggioFine = corsa.ParcheggioDestinazione?.Nome
            };
        }

        private async Task<CorsaDto> MapToDtoWithDetails(Corsa corsa)
        {
            var mezzo = corsa.Mezzo ?? await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            
            return new CorsaDto
            {
                Id = corsa.Id,
                UtenteId = corsa.UtenteId,
                MezzoId = corsa.MezzoId,
                ParcheggioPartenzaId = corsa.ParcheggioPartenzaId,
                ParcheggioDestinazioneId = corsa.ParcheggioDestinazioneId,
                Inizio = corsa.Inizio,
                Fine = corsa.Fine,
                DurataMinuti = corsa.DurataMinuti,
                CostoTotale = corsa.CostoTotale,
                Stato = corsa.Stato.ToString(),
                MezzoModello = mezzo?.Modello ?? "N/A",
                MezzoTipo = mezzo?.Tipo.ToString() ?? "N/A", 
                TariffaPerMinuto = mezzo?.TariffaPerMinuto ?? 0,
                TariffaFissa = mezzo?.TariffaFissa ?? 1.00m,
                IsElettrico = mezzo?.IsElettrico ?? false,
                PuntiEcoAssegnati = corsa.PuntiEcoAssegnati,
                NomeParcheggioInizio = corsa.ParcheggioPartenza?.Nome ?? "N/A",
                NomeParcheggioFine = corsa.ParcheggioDestinazione?.Nome
            };
        }

        private async Task CreaSegnalazioneManutenzione(Corsa corsa, string? descrizione)
        {
            var segnalazione = new SegnalazioneManutenzione
            {
                MezzoId = corsa.MezzoId,
                UtenteId = corsa.UtenteId,
                CorsaId = corsa.Id,
                Descrizione = descrizione ?? "Segnalazione generica durante la corsa",
                Stato = StatoSegnalazione.Aperta,
                Priorita = PrioritaSegnalazione.Media,
                DataSegnalazione = DateTime.UtcNow
            };

            await _segnalazioneRepository.AddAsync(segnalazione);
            _logger.LogInformation("Created maintenance report {SegnalazioneId} for Mezzo {MezzoId} by User {UserId}", 
                segnalazione.Id, corsa.MezzoId, corsa.UtenteId);
        }
    }
}