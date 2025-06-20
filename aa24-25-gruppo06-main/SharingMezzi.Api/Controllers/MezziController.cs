using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MezziController : ControllerBase
    {
        private readonly IMezzoService _mezzoService;

        public MezziController(IMezzoService mezzoService)
        {
            _mezzoService = mezzoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MezzoDto>>> GetMezzi()
        {
            var mezzi = await _mezzoService.GetAllMezziAsync();
            return Ok(mezzi);
        }

        [HttpGet("disponibili")]
        public async Task<ActionResult<IEnumerable<MezzoDto>>> GetMezziDisponibili()
        {
            var mezzi = await _mezzoService.GetMezziDisponibiliAsync();
            return Ok(mezzi);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MezzoDto>> GetMezzo(int id)
        {
            var mezzo = await _mezzoService.GetMezzoByIdAsync(id);
            if (mezzo == null)
                return NotFound();

            return Ok(mezzo);
        }

        [HttpPost]
        public async Task<ActionResult<MezzoDto>> CreateMezzo([FromBody] CreateMezzoDto createDto)
        {
            var mezzo = await _mezzoService.CreateMezzoAsync(createDto);
            return CreatedAtAction(nameof(GetMezzo), new { id = mezzo.Id }, mezzo);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<MezzoDto>> UpdateMezzo(int id, [FromBody] MezzoDto mezzoDto)
        {
            try
            {
                var mezzo = await _mezzoService.UpdateMezzoAsync(id, mezzoDto);
                return Ok(mezzo);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        [HttpPut("{id}/batteria")]
        public async Task<IActionResult> UpdateBatteria(int id, [FromBody] int livelloBatteria)
        {
            await _mezzoService.UpdateBatteryAsync(id, livelloBatteria);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMezzo(int id)
        {
            await _mezzoService.DeleteMezzoAsync(id);
            return NoContent();
        }
    }
}