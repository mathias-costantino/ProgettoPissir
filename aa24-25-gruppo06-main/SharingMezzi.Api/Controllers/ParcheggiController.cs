using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParcheggiController : ControllerBase
    {
        private readonly IParcheggioService _parcheggioService;

        public ParcheggiController(IParcheggioService parcheggioService)
        {
            _parcheggioService = parcheggioService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ParcheggioDto>>> GetParcheggi()
        {
            var parcheggi = await _parcheggioService.GetAllParcheggiAsync();
            return Ok(parcheggi);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ParcheggioDto>> GetParcheggio(int id)
        {
            var parcheggio = await _parcheggioService.GetParcheggioByIdAsync(id);
            if (parcheggio == null)
                return NotFound();

            return Ok(parcheggio);
        }

        [HttpPost]
        public async Task<ActionResult<ParcheggioDto>> CreateParcheggio([FromBody] CreateParcheggioDto createDto)
        {
            var parcheggio = await _parcheggioService.CreateParcheggioAsync(createDto);
            return CreatedAtAction(nameof(GetParcheggio), new { id = parcheggio.Id }, parcheggio);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ParcheggioDto>> UpdateParcheggio(int id, [FromBody] ParcheggioDto parcheggioDto)
        {
            try
            {
                var parcheggio = await _parcheggioService.UpdateParcheggioAsync(id, parcheggioDto);
                return Ok(parcheggio);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParcheggio(int id)
        {
            await _parcheggioService.DeleteParcheggioAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/update-posti")]
        public async Task<IActionResult> UpdatePostiLiberi(int id)
        {
            await _parcheggioService.UpdatePostiLiberiAsync(id);
            return NoContent();
        }
    }
}