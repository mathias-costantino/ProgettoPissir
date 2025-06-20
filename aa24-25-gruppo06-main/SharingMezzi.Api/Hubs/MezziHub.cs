using Microsoft.AspNetCore.SignalR;
using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Api.Hubs
{
    /// <summary>
    /// Hub SignalR per eventi real-time sui mezzi
    /// </summary>
    public class MezziHub : Hub
    {
        private readonly ILogger<MezziHub> _logger;

        public MezziHub(ILogger<MezziHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client si iscrive agli aggiornamenti di un mezzo specifico
        /// </summary>
        public async Task SubscribeToMezzo(int mezzoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"mezzo_{mezzoId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to mezzo {MezzoId}", 
                Context.ConnectionId, mezzoId);
        }

        /// <summary>
        /// Client si disiscrive dagli aggiornamenti di un mezzo
        /// </summary>
        public async Task UnsubscribeFromMezzo(int mezzoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"mezzo_{mezzoId}");
            _logger.LogInformation("Client {ConnectionId} unsubscribed from mezzo {MezzoId}", 
                Context.ConnectionId, mezzoId);
        }

        /// <summary>
        /// Client si iscrive agli aggiornamenti di tutti i mezzi in un parcheggio
        /// </summary>
        public async Task SubscribeToParcheggio(int parcheggioId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"parcheggio_mezzi_{parcheggioId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to mezzi in parcheggio {ParcheggioId}", 
                Context.ConnectionId, parcheggioId);
        }

        /// <summary>
        /// Client amministratore si iscrive a tutti i mezzi del sistema
        /// </summary>
        public async Task SubscribeToAllMezzi()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_mezzi");
            _logger.LogInformation("Admin client {ConnectionId} subscribed to all mezzi", Context.ConnectionId);
        }

        /// <summary>
        /// Gestisce disconnessione client
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from MezziHub", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gestisce connessione client
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to MezziHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }

    /// <summary>
    /// Servizio per inviare notifiche sui mezzi tramite SignalR
    /// </summary>
    public interface IMezziNotificationService
    {
        Task NotifyMezzoStatusChanged(int mezzoId, MezzoDto mezzo);
        Task NotifyMezzoBatteryLow(int mezzoId, int batteryLevel);
        Task NotifyMezzoMovement(int mezzoId, double latitude, double longitude);
        Task NotifyMezzoLocked(int mezzoId);
        Task NotifyMezzoUnlocked(int mezzoId);
        Task NotifyMezzoAlarm(int mezzoId, string alarmType);
        Task NotifyParcheggioMezziUpdate(int parcheggioId, IEnumerable<MezzoDto> mezzi);
    }

    public class MezziNotificationService : IMezziNotificationService
    {
        private readonly IHubContext<MezziHub> _hubContext;
        private readonly ILogger<MezziNotificationService> _logger;

        public MezziNotificationService(IHubContext<MezziHub> hubContext, ILogger<MezziNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyMezzoStatusChanged(int mezzoId, MezzoDto mezzo)
        {
            await _hubContext.Clients.Groups($"mezzo_{mezzoId}", "all_mezzi")
                .SendAsync("MezzoStatusChanged", mezzo);
            _logger.LogDebug("Notified status change for mezzo {MezzoId}: {Status}", mezzoId, mezzo.Stato);
        }

        public async Task NotifyMezzoBatteryLow(int mezzoId, int batteryLevel)
        {
            await _hubContext.Clients.Groups($"mezzo_{mezzoId}", "all_mezzi")
                .SendAsync("MezzoBatteryLow", new { MezzoId = mezzoId, BatteryLevel = batteryLevel });
            _logger.LogWarning("Notified low battery for mezzo {MezzoId}: {BatteryLevel}%", mezzoId, batteryLevel);
        }

        public async Task NotifyMezzoMovement(int mezzoId, double latitude, double longitude)
        {
            await _hubContext.Clients.Group($"mezzo_{mezzoId}")
                .SendAsync("MezzoMovement", new { MezzoId = mezzoId, Latitude = latitude, Longitude = longitude });
            _logger.LogDebug("Notified movement for mezzo {MezzoId}", mezzoId);
        }

        public async Task NotifyMezzoLocked(int mezzoId)
        {
            await _hubContext.Clients.Groups($"mezzo_{mezzoId}", "all_mezzi")
                .SendAsync("MezzoLocked", new { MezzoId = mezzoId });
            _logger.LogInformation("Notified lock for mezzo {MezzoId}", mezzoId);
        }

        public async Task NotifyMezzoUnlocked(int mezzoId)
        {
            await _hubContext.Clients.Groups($"mezzo_{mezzoId}", "all_mezzi")
                .SendAsync("MezzoUnlocked", new { MezzoId = mezzoId });
            _logger.LogInformation("Notified unlock for mezzo {MezzoId}", mezzoId);
        }

        public async Task NotifyMezzoAlarm(int mezzoId, string alarmType)
        {
            await _hubContext.Clients.Groups($"mezzo_{mezzoId}", "all_mezzi")
                .SendAsync("MezzoAlarm", new { MezzoId = mezzoId, AlarmType = alarmType });
            _logger.LogWarning("Notified alarm for mezzo {MezzoId}: {AlarmType}", mezzoId, alarmType);
        }

        public async Task NotifyParcheggioMezziUpdate(int parcheggioId, IEnumerable<MezzoDto> mezzi)
        {
            await _hubContext.Clients.Group($"parcheggio_mezzi_{parcheggioId}")
                .SendAsync("ParcheggioMezziUpdate", mezzi);
            _logger.LogDebug("Notified mezzi update for parcheggio {ParcheggioId}", parcheggioId);
        }
    }
}