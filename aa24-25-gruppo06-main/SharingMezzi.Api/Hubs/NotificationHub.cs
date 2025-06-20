using Microsoft.AspNetCore.SignalR;
using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Api.Hubs
{
    /// <summary>
    /// Hub SignalR per notifiche real-time agli utenti
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Utente si connette e entra nel gruppo del suo ID
        /// </summary>
        public async Task JoinUserGroup(int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} joined personal group with connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        /// <summary>
        /// Utente si disconnette dal gruppo
        /// </summary>
        public async Task LeaveUserGroup(int userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} left personal group", userId);
        }

        /// <summary>
        /// Amministratore entra nel gruppo admin per notifiche di sistema
        /// </summary>
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "administrators");
            _logger.LogInformation("Admin joined with connection {ConnectionId}", Context.ConnectionId);
        }

        /// <summary>
        /// Monitoraggio parcheggio specifico
        /// </summary>
        public async Task JoinParkingGroup(int parkingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"parking_{parkingId}");
            _logger.LogInformation("Client joined parking {ParkingId} monitoring", parkingId);
        }

        /// <summary>
        /// Gestisce disconnessione client
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gestisce connessione client
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }

    /// <summary>
    /// Servizio per inviare notifiche tramite SignalR
    /// </summary>
    public interface INotificationService
    {
        Task SendToUser(int userId, string method, object data);
        Task SendToAdmins(string method, object data);
        Task SendToParkingMonitors(int parkingId, string method, object data);
        Task SendToAll(string method, object data);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendToUser(int userId, string method, object data)
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync(method, data);
            _logger.LogDebug("Sent {Method} to user {UserId}", method, userId);
        }

        public async Task SendToAdmins(string method, object data)
        {
            await _hubContext.Clients.Group("administrators").SendAsync(method, data);
            _logger.LogDebug("Sent {Method} to administrators", method);
        }

        public async Task SendToParkingMonitors(int parkingId, string method, object data)
        {
            await _hubContext.Clients.Group($"parking_{parkingId}").SendAsync(method, data);
            _logger.LogDebug("Sent {Method} to parking {ParkingId} monitors", method, parkingId);
        }

        public async Task SendToAll(string method, object data)
        {
            await _hubContext.Clients.All.SendAsync(method, data);
            _logger.LogDebug("Sent {Method} to all clients", method);
        }
    }
}