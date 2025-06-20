using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace SharingMezzi.Api.Controllers
{
    /// <summary>
    /// Controller per gestione e monitoraggio batterie mezzi elettrici
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BatteryController : ControllerBase
    {
        private readonly BatteryEmulatorService _batteryEmulator;
        private readonly ILogger<BatteryController> _logger;

        public BatteryController(
            BatteryEmulatorService batteryEmulator,
            ILogger<BatteryController> logger)
        {
            _batteryEmulator = batteryEmulator;
            _logger = logger;
        }

        /// <summary>
        /// Ottiene lo stato batteria di un mezzo specifico
        /// </summary>
        [HttpGet("{mezzoId}")]
        public IActionResult GetBatteryStatus(int mezzoId)
        {
            try
            {
                var batteryStatus = _batteryEmulator.GetBatteryStatus(mezzoId);
                
                if (batteryStatus == null)
                {
                    return NotFound(new { Message = $"Stato batteria non disponibile per Mezzo {mezzoId}" });
                }

                return Ok(batteryStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting battery status for Mezzo {MezzoId}", mezzoId);
                return StatusCode(500, new { Error = "Errore nel recupero stato batteria" });
            }
        }

        /// <summary>
        /// Ottiene tutti gli stati batteria
        /// </summary>
        [HttpGet("all")]
        public IActionResult GetAllBatteryStatuses()
        {
            try
            {
                var batteryStatuses = _batteryEmulator.GetAllBatteryStatuses().ToList();
                
                var summary = new
                {
                    TotalElectricVehicles = batteryStatuses.Count,
                    LowBatteryCount = batteryStatuses.Count(b => b.BatteryLevel < 20),
                    ChargingCount = batteryStatuses.Count(b => b.IsCharging),
                    AverageBatteryLevel = batteryStatuses.Any() ? batteryStatuses.Average(b => b.BatteryLevel) : 0,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(new
                {
                    Summary = summary,
                    BatteryStatuses = batteryStatuses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all battery statuses");
                return StatusCode(500, new { Error = "Errore nel recupero stati batteria" });
            }
        }

        /// <summary>
        /// Imposta manualmente il livello batteria per testing
        /// </summary>
        [HttpPost("{mezzoId}/set-level")]
        [Authorize(Roles = "Amministratore")]
        public IActionResult SetBatteryLevel(int mezzoId, [FromBody] SetBatteryLevelDto request)
        {
            try
            {
                if (request.Level < 0 || request.Level > 100)
                {
                    return BadRequest(new { Message = "Il livello batteria deve essere tra 0 e 100" });
                }

                _batteryEmulator.SetBatteryLevel(mezzoId, request.Level);
                
                _logger.LogInformation("Battery level manually set for Mezzo {MezzoId}: {Level}%", 
                    mezzoId, request.Level);

                return Ok(new
                {
                    Success = true,
                    Message = $"Livello batteria impostato a {request.Level}% per Mezzo {mezzoId}",
                    MezzoId = mezzoId,
                    NewLevel = request.Level,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting battery level for Mezzo {MezzoId}", mezzoId);
                return StatusCode(500, new { Error = "Errore nell'impostazione livello batteria" });
            }
        }

        /// <summary>
        /// Ottiene mezzi con batteria bassa (sotto il 20%)
        /// </summary>
        [HttpGet("low-battery")]
        public IActionResult GetLowBatteryVehicles()
        {
            try
            {
                var lowBatteryVehicles = _batteryEmulator.GetAllBatteryStatuses()
                    .Where(b => b.BatteryLevel < 20)
                    .OrderBy(b => b.BatteryLevel)
                    .ToList();

                return Ok(new
                {
                    Count = lowBatteryVehicles.Count,
                    Vehicles = lowBatteryVehicles,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low battery vehicles");
                return StatusCode(500, new { Error = "Errore nel recupero veicoli batteria bassa" });
            }
        }
    }

    /// <summary>
    /// DTO per impostazione livello batteria
    /// </summary>
    public class SetBatteryLevelDto
    {
        public int Level { get; set; }
    }
}
