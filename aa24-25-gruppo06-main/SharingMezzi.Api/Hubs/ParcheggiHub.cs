using Microsoft.AspNetCore.SignalR;
using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Api.Hubs
{
    /// <summary>
    /// Hub SignalR per eventi real-time sui parcheggi
    /// </summary>
    public class ParcheggiHub : Hub
    {
        private readonly ILogger<ParcheggiHub> _logger;

        public ParcheggiHub(ILogger<ParcheggiHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client si iscrive agli aggiornamenti di un parcheggio specifico
        /// </summary>
        public async Task SubscribeToParcheggio(int parcheggioId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"parcheggio_{parcheggioId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to parcheggio {ParcheggioId}", 
                Context.ConnectionId, parcheggioId);
        }

        /// <summary>
        /// Client si disiscrive dagli aggiornamenti di un parcheggio
        /// </summary>
        public async Task UnsubscribeFromParcheggio(int parcheggioId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"parcheggio_{parcheggioId}");
            _logger.LogInformation("Client {ConnectionId} unsubscribed from parcheggio {ParcheggioId}", 
                Context.ConnectionId, parcheggioId);
        }

        /// <summary>
        /// Client si iscrive agli aggiornamenti di tutti i parcheggi
        /// </summary>
        public async Task SubscribeToAllParcheggi()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_parcheggi");
            _logger.LogInformation("Client {ConnectionId} subscribed to all parcheggi", Context.ConnectionId);
        }

        /// <summary>
        /// Client si iscrive alle statistiche dei parcheggi (per dashboard)
        /// </summary>
        public async Task SubscribeToParcheggiStats()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "parcheggi_stats");
            _logger.LogInformation("Client {ConnectionId} subscribed to parcheggi statistics", Context.ConnectionId);
        }

        /// <summary>
        /// Client si iscrive ai parcheggi in una zona geografica specifica
        /// </summary>
        public async Task SubscribeToAreaParcheggi(string area)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"area_{area}");
            _logger.LogInformation("Client {ConnectionId} subscribed to area {Area} parcheggi", 
                Context.ConnectionId, area);
        }

        /// <summary>
        /// Gestisce disconnessione client
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from ParcheggiHub", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gestisce connessione client
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to ParcheggiHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }

    /// <summary>
    /// Servizio per inviare notifiche sui parcheggi tramite SignalR
    /// </summary>
    public interface IParcheggiNotificationService
    {
        Task NotifyParcheggioCapacityChanged(int parcheggioId, ParcheggioDto parcheggio);
        Task NotifyParcheggioStatusChanged(int parcheggioId, ParcheggioDto parcheggio);
        Task NotifyMezzoAggiunto(int parcheggioId, MezzoDto mezzo);
        Task NotifyMezzoRimosso(int parcheggioId, MezzoDto mezzo);
        Task NotifyParcheggioFull(int parcheggioId);
        Task NotifyParcheggioEmpty(int parcheggioId);
        Task NotifySlotStatusChanged(int parcheggioId, int slotId, bool isOccupied);
        Task NotifyParcheggiStats(Dictionary<string, object> stats);
        Task NotifyMaintenanceAlert(int parcheggioId, string message);
    }

    public class ParcheggiNotificationService : IParcheggiNotificationService
    {
        private readonly IHubContext<ParcheggiHub> _hubContext;
        private readonly ILogger<ParcheggiNotificationService> _logger;

        public ParcheggiNotificationService(IHubContext<ParcheggiHub> hubContext, ILogger<ParcheggiNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyParcheggioCapacityChanged(int parcheggioId, ParcheggioDto parcheggio)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi", "parcheggi_stats")
                .SendAsync("ParcheggioCapacityChanged", parcheggio);
            _logger.LogInformation("Notified capacity change for parcheggio {ParcheggioId}: {Liberi}/{Capienza}", 
                parcheggioId, parcheggio.PostiLiberi, parcheggio.Capienza);
        }

        public async Task NotifyParcheggioStatusChanged(int parcheggioId, ParcheggioDto parcheggio)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi")
                .SendAsync("ParcheggioStatusChanged", parcheggio);
            _logger.LogDebug("Notified status change for parcheggio {ParcheggioId}", parcheggioId);
        }

        public async Task NotifyMezzoAggiunto(int parcheggioId, MezzoDto mezzo)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi")
                .SendAsync("MezzoAggiunto", new { ParcheggioId = parcheggioId, Mezzo = mezzo });
            _logger.LogInformation("Notified mezzo {MezzoId} added to parcheggio {ParcheggioId}", 
                mezzo.Id, parcheggioId);
        }

        public async Task NotifyMezzoRimosso(int parcheggioId, MezzoDto mezzo)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi")
                .SendAsync("MezzoRimosso", new { ParcheggioId = parcheggioId, Mezzo = mezzo });
            _logger.LogInformation("Notified mezzo {MezzoId} removed from parcheggio {ParcheggioId}", 
                mezzo.Id, parcheggioId);
        }

        public async Task NotifyParcheggioFull(int parcheggioId)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi", "parcheggi_stats")
                .SendAsync("ParcheggioFull", new { ParcheggioId = parcheggioId });
            _logger.LogWarning("Notified parcheggio {ParcheggioId} is FULL", parcheggioId);
        }

        public async Task NotifyParcheggioEmpty(int parcheggioId)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi", "parcheggi_stats")
                .SendAsync("ParcheggioEmpty", new { ParcheggioId = parcheggioId });
            _logger.LogInformation("Notified parcheggio {ParcheggioId} is EMPTY", parcheggioId);
        }

        public async Task NotifySlotStatusChanged(int parcheggioId, int slotId, bool isOccupied)
        {
            await _hubContext.Clients.Group($"parcheggio_{parcheggioId}")
                .SendAsync("SlotStatusChanged", new { 
                    ParcheggioId = parcheggioId, 
                    SlotId = slotId, 
                    IsOccupied = isOccupied 
                });
            _logger.LogDebug("Notified slot {SlotId} status change: {Status}", slotId, isOccupied ? "OCCUPIED" : "FREE");
        }

        public async Task NotifyParcheggiStats(Dictionary<string, object> stats)
        {
            await _hubContext.Clients.Group("parcheggi_stats")
                .SendAsync("ParcheggiStatsUpdate", stats);
            _logger.LogDebug("Notified parcheggi statistics update");
        }

        public async Task NotifyMaintenanceAlert(int parcheggioId, string message)
        {
            await _hubContext.Clients.Groups($"parcheggio_{parcheggioId}", "all_parcheggi")
                .SendAsync("MaintenanceAlert", new { ParcheggioId = parcheggioId, Message = message });
            _logger.LogWarning("Notified maintenance alert for parcheggio {ParcheggioId}: {Message}", 
                parcheggioId, message);
        }
    }
}