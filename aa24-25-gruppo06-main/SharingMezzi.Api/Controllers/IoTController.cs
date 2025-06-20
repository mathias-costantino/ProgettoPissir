using Microsoft.AspNetCore.Mvc;
using SharingMezzi.IoT.Services;

namespace SharingMezzi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IoTController : ControllerBase
    {
        private readonly ConnectedIoTClientsService _iotClientsService;

        public IoTController(ConnectedIoTClientsService iotClientsService)
        {
            _iotClientsService = iotClientsService;
        }

        [HttpGet("clients")]
        public IActionResult GetClients()
        {
            var clients = _iotClientsService.GetAllConnectedClients();
            return Ok(clients.Select(c => new
            {
                MezzoId = c.State.MezzoId,
                IsConnected = c.IsConnected,
                LastHeartbeat = c.State.LastOperation,
                BatteryLevel = c.State.BatteryLevel,
                IsMoving = c.State.IsMoving,
                LockState = c.State.LockState
            }));
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var clients = _iotClientsService.GetAllConnectedClients();
            return Ok(new
            {
                TotalClients = clients.Count(),
                ConnectedClients = clients.Count(c => c.IsConnected),
                Timestamp = DateTime.UtcNow
            });
        }
    }
}