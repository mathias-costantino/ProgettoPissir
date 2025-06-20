using Microsoft.AspNetCore.SignalR;
using SharingMezzi.Core.DTOs;

namespace SharingMezzi.Api.Hubs
{
    /// <summary>
    /// Hub SignalR per eventi real-time sulle corse
    /// </summary>
    public class CorseHub : Hub
    {
        private readonly ILogger<CorseHub> _logger;

        public CorseHub(ILogger<CorseHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client si iscrive agli aggiornamenti di una corsa specifica
        /// </summary>
        public async Task SubscribeToCorsa(int corsaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"corsa_{corsaId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to corsa {CorsaId}", 
                Context.ConnectionId, corsaId);
        }

        /// <summary>
        /// Client si disiscrive dagli aggiornamenti di una corsa
        /// </summary>
        public async Task UnsubscribeFromCorsa(int corsaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"corsa_{corsaId}");
            _logger.LogInformation("Client {ConnectionId} unsubscribed from corsa {CorsaId}", 
                Context.ConnectionId, corsaId);
        }

        /// <summary>
        /// Client utente si iscrive alle proprie corse
        /// </summary>
        public async Task SubscribeToUserCorse(int utenteId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_corse_{utenteId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to corse for user {UtenteId}", 
                Context.ConnectionId, utenteId);
        }

        /// <summary>
        /// Client amministratore si iscrive a tutte le corse del sistema
        /// </summary>
        public async Task SubscribeToAllCorse()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_corse");
            _logger.LogInformation("Admin client {ConnectionId} subscribed to all corse", Context.ConnectionId);
        }

        /// <summary>
        /// Client si iscrive agli aggiornamenti delle corse per un mezzo specifico
        /// </summary>
        public async Task SubscribeToMezzoCorse(int mezzoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"mezzo_corse_{mezzoId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to corse for mezzo {MezzoId}", 
                Context.ConnectionId, mezzoId);
        }

        /// <summary>
        /// Gestisce disconnessione client
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from CorseHub", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gestisce connessione client
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to CorseHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }

    /// <summary>
    /// Servizio per inviare notifiche sulle corse tramite SignalR
    /// </summary>
    public interface ICorseNotificationService
    {
        Task NotifyCorsaStarted(int corsaId, CorsaDto corsa);
        Task NotifyCorsaEnded(int corsaId, CorsaDto corsa);
        Task NotifyCorsaUpdated(int corsaId, CorsaDto corsa);
        Task NotifyCorsaLocationUpdate(int corsaId, double latitude, double longitude);
        Task NotifyUserCorseUpdate(int utenteId, IEnumerable<CorsaDto> corse);
        Task NotifyCorsaCostUpdate(int corsaId, decimal newCost);
        Task NotifyMezzoCorsaStarted(int mezzoId, CorsaDto corsa);
        Task NotifyMezzoCorsaEnded(int mezzoId, CorsaDto corsa);
    }

    public class CorseNotificationService : ICorseNotificationService
    {
        private readonly IHubContext<CorseHub> _hubContext;
        private readonly ILogger<CorseNotificationService> _logger;

        public CorseNotificationService(IHubContext<CorseHub> hubContext, ILogger<CorseNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyCorsaStarted(int corsaId, CorsaDto corsa)
        {
            await _hubContext.Clients.Groups($"corsa_{corsaId}", $"user_corse_{corsa.UtenteId}", "all_corse")
                .SendAsync("CorsaStarted", corsa);
            _logger.LogInformation("Notified start of corsa {CorsaId} for user {UtenteId}", corsaId, corsa.UtenteId);
        }

        public async Task NotifyCorsaEnded(int corsaId, CorsaDto corsa)
        {
            await _hubContext.Clients.Groups($"corsa_{corsaId}", $"user_corse_{corsa.UtenteId}", "all_corse")
                .SendAsync("CorsaEnded", corsa);
            _logger.LogInformation("Notified end of corsa {CorsaId} - Cost: {Cost:C}", corsaId, corsa.CostoTotale);
        }

        public async Task NotifyCorsaUpdated(int corsaId, CorsaDto corsa)
        {
            await _hubContext.Clients.Groups($"corsa_{corsaId}", $"user_corse_{corsa.UtenteId}", "all_corse")
                .SendAsync("CorsaUpdated", corsa);
            _logger.LogDebug("Notified update for corsa {CorsaId}", corsaId);
        }

        public async Task NotifyCorsaLocationUpdate(int corsaId, double latitude, double longitude)
        {
            await _hubContext.Clients.Group($"corsa_{corsaId}")
                .SendAsync("CorsaLocationUpdate", new { CorsaId = corsaId, Latitude = latitude, Longitude = longitude });
            _logger.LogDebug("Notified location update for corsa {CorsaId}", corsaId);
        }

        public async Task NotifyUserCorseUpdate(int utenteId, IEnumerable<CorsaDto> corse)
        {
            await _hubContext.Clients.Group($"user_corse_{utenteId}")
                .SendAsync("UserCorseUpdate", corse);
            _logger.LogDebug("Notified corse update for user {UtenteId}", utenteId);
        }

        public async Task NotifyCorsaCostUpdate(int corsaId, decimal newCost)
        {
            await _hubContext.Clients.Group($"corsa_{corsaId}")
                .SendAsync("CorsaCostUpdate", new { CorsaId = corsaId, NewCost = newCost });
            _logger.LogDebug("Notified cost update for corsa {CorsaId}: {Cost:C}", corsaId, newCost);
        }

        public async Task NotifyMezzoCorsaStarted(int mezzoId, CorsaDto corsa)
        {
            await _hubContext.Clients.Group($"mezzo_corse_{mezzoId}")
                .SendAsync("MezzoCorsaStarted", corsa);
            _logger.LogInformation("Notified corsa started for mezzo {MezzoId}", mezzoId);
        }

        public async Task NotifyMezzoCorsaEnded(int mezzoId, CorsaDto corsa)
        {
            await _hubContext.Clients.Group($"mezzo_corse_{mezzoId}")
                .SendAsync("MezzoCorsaEnded", corsa);
            _logger.LogInformation("Notified corsa ended for mezzo {MezzoId}", mezzoId);
        }
    }
}