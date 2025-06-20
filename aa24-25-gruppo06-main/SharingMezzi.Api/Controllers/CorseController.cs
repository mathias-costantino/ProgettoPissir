using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CorseController : ControllerBase
    {
        private readonly ICorsaService _corsaService;

        public CorseController(ICorsaService corsaService)
        {
            _corsaService = corsaService;
        }

        [HttpPost("inizia")]
        public async Task<ActionResult<CorsaDto>> IniziaCorsa([FromBody] IniziaCorsa comando)
        {
            try
            {
                var corsa = await _corsaService.IniziaCorsa(comando);
                return Ok(corsa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Avvia una corsa con controllo batteria avanzato
        /// </summary>
        [HttpPost("avvia")]
        public async Task<ActionResult<AvviaCorseResponseDto>> AvviaCorsa([FromBody] IniziaCorsa comando)
        {
            try
            {
                // Prima controlla il livello di batteria per mezzi elettrici
                var (batteryAcceptable, batteryLevel) = await _corsaService.CheckBatteryLevel(comando.MezzoId);
                
                if (!batteryAcceptable && batteryLevel.HasValue)
                {
                    return BadRequest(new AvviaCorseResponseDto
                    {
                        CorsaId = 0,
                        MezzoSbloccato = false,
                        Message = $"Livello batteria insufficiente ({batteryLevel}%). È richiesto almeno il 20% per iniziare una corsa.",
                        BatteryLevel = batteryLevel,
                        RequiredBatteryCheck = true
                    });
                }

                // Se batteria OK, avvia la corsa
                var corsa = await _corsaService.IniziaCorsa(comando);
                
                return Ok(new AvviaCorseResponseDto
                {
                    CorsaId = corsa.Id,
                    MezzoSbloccato = true,
                    Message = "Corsa avviata con successo. Mezzo sbloccato.",
                    BatteryLevel = batteryLevel,
                    RequiredBatteryCheck = batteryLevel.HasValue
                });
            }
            catch (InvalidOperationException ex)
            {
                // Anche se la corsa fallisce per altri motivi, ottieni comunque il livello batteria
                var (batteryAcceptable, batteryLevel) = await _corsaService.CheckBatteryLevel(comando.MezzoId);
                
                return BadRequest(new AvviaCorseResponseDto
                {
                    CorsaId = 0,
                    MezzoSbloccato = false,
                    Message = ex.Message,
                    BatteryLevel = batteryLevel,
                    RequiredBatteryCheck = batteryLevel.HasValue
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AvviaCorseResponseDto
                {
                    CorsaId = 0,
                    MezzoSbloccato = false,
                    Message = "Errore interno del server",
                    RequiredBatteryCheck = false
                });
            }
        }

        [HttpPut("{corsaId}/termina")]
        public async Task<ActionResult<CorsaDto>> TerminaCorsa(int corsaId, [FromBody] TerminaCorsa comando)
        {
            try
            {
                var corsa = await _corsaService.TerminaCorsa(corsaId, comando);
                return Ok(corsa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Endpoint di compatibilità per il vecchio comportamento
        [HttpPost("{corsaId}/termina")]
        public async Task<ActionResult<CorsaDto>> TerminaCorsaLegacy(int corsaId, [FromQuery] int? parcheggioDestinazioneId = null)
        {
            try
            {
                var comando = new TerminaCorsa
                {
                    ParcheggioDestinazioneId = parcheggioDestinazioneId ?? 1,
                    SegnalaManutenzione = false
                };
                var corsa = await _corsaService.TerminaCorsa(corsaId, comando);
                return Ok(corsa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("utente/{utenteId}")]
        public async Task<ActionResult<IEnumerable<CorsaDto>>> GetCorseUtente(int utenteId)
        {
            var corse = await _corsaService.GetCorseUtente(utenteId);
            return Ok(corse);
        }

        [HttpGet("utente/{utenteId}/attive")]
        public async Task<ActionResult<IEnumerable<CorsaDto>>> GetCorseAttive(int utenteId)
        {
            var corse = await _corsaService.GetCorseUtente(utenteId);
            var corseAttive = corse.Where(c => c.Stato == "InCorso").ToList();
            return Ok(corseAttive);
        }

        [HttpGet("utente/{utenteId}/cronologia")]
        public async Task<ActionResult<IEnumerable<CorsaDto>>> GetCronologiaCorse(int utenteId)
        {
            var corse = await _corsaService.GetCorseUtente(utenteId);
            var cronologia = corse.Where(c => c.Stato == "Completata").ToList();
            return Ok(cronologia);
        }

        [HttpGet("{corsaId}")]
        public async Task<ActionResult<CorsaDto>> GetCorsa(int corsaId)
        {
            try
            {
                var corse = await _corsaService.GetCorseUtente(0); // Get all for admin or implement GetCorsa method
                var corsa = corse.FirstOrDefault(c => c.Id == corsaId);
                
                if (corsa == null)
                    return NotFound("Corsa non trovata");
                    
                return Ok(corsa);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("utente/{utenteId}/attiva")]
        public async Task<ActionResult<CorsaDto?>> GetCorsaAttiva(int utenteId)
        {
            var corsa = await _corsaService.GetCorsaAttiva(utenteId);
            return Ok(corsa);
        }
    }
}