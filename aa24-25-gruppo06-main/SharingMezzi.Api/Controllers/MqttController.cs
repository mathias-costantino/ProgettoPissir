using Microsoft.AspNetCore.Mvc;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MqttController : ControllerBase
    {
        private readonly IMqttService _mqttService;

        public MqttController(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        [HttpGet("status")]
        public IActionResult GetMqttStatus()
        {
            return Ok(new { IsConnected = _mqttService.IsConnected, Timestamp = DateTime.UtcNow });
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishMessage([FromBody] PublishMessageDto messageDto)
        {
            try
            {
                await _mqttService.PublishAsync(messageDto.Topic, messageDto.Message);
                return Ok(new { Success = true, Topic = messageDto.Topic, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("unlock/{mezzoId}")]
        public async Task<IActionResult> UnlockMezzo(int mezzoId)
        {
            try
            {
                var topic = $"parking/1/stato_mezzi/{mezzoId}";
                var command = new { action = "unlock", mezzoId = mezzoId, timestamp = DateTime.UtcNow };
                await _mqttService.PublishAsync(topic, command);
                return Ok(new { Success = true, MezzoId = mezzoId, Action = "unlock", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("lock/{mezzoId}")]
        public async Task<IActionResult> LockMezzo(int mezzoId)
        {
            try
            {
                var topic = $"parking/1/stato_mezzi/{mezzoId}";
                var command = new { action = "lock", mezzoId = mezzoId, timestamp = DateTime.UtcNow };
                await _mqttService.PublishAsync(topic, command);
                return Ok(new { Success = true, MezzoId = mezzoId, Action = "lock", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("led/{slotId}")]
        public async Task<IActionResult> ControlLed(int slotId, [FromBody] LedControlDto ledDto)
        {
            try
            {
                var topic = $"parking/1/attuatori/led/{slotId}";
                var command = new { color = ledDto.Color, pattern = ledDto.Pattern, timestamp = DateTime.UtcNow };
                await _mqttService.PublishAsync(topic, command);
                return Ok(new { 
                    Success = true, 
                    SlotId = slotId, 
                    Color = ledDto.Color, 
                    Pattern = ledDto.Pattern,
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class PublishMessageDto
    {
        public string Topic { get; set; } = string.Empty;
        public object Message { get; set; } = new();
    }

    public class LedControlDto
    {
        public string Color { get; set; } = "green";
        public string Pattern { get; set; } = "solid";
    }
}