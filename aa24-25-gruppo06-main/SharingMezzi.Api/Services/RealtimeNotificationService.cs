using SharingMezzi.Api.Hubs;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using Microsoft.Extensions.Logging;

namespace SharingMezzi.Api.Services
{
    /// <summary>
    /// Servizio che ascolta eventi MQTT e li trasforma in notifiche SignalR
    /// </summary>
    public class RealtimeNotificationService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<RealtimeNotificationService> _logger;

        public RealtimeNotificationService(
            INotificationService notificationService,
            ILogger<RealtimeNotificationService> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Notifica inizio corsa all'utente
        /// </summary>
        public async Task NotifyRideStarted(CorsaDto corsa)
        {
            var notification = new
            {
                Type = "ride_started",
                Message = $"Corsa iniziata con mezzo {corsa.MezzoId}",
                CorsaId = corsa.Id,
                MezzoId = corsa.MezzoId,
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToUser(corsa.UtenteId, "RideNotification", notification);
            _logger.LogInformation("Notified user {UserId} of ride start", corsa.UtenteId);
        }

        /// <summary>
        /// Notifica fine corsa e costo
        /// </summary>
        public async Task NotifyRideEnded(CorsaDto corsa)
        {
            var notification = new
            {
                Type = "ride_ended",
                Message = $"Corsa terminata. Costo: €{corsa.CostoTotale:F2}",
                CorsaId = corsa.Id,
                Duration = corsa.DurataMinuti,
                Cost = corsa.CostoTotale,
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToUser(corsa.UtenteId, "RideNotification", notification);
            _logger.LogInformation("Notified user {UserId} of ride end, cost: €{Cost}", 
                corsa.UtenteId, corsa.CostoTotale);
        }

        /// <summary>
        /// Notifica batteria scarica di un mezzo agli amministratori
        /// </summary>
        public async Task NotifyLowBattery(int mezzoId, int batteryLevel, int parkingId)
        {
            var notification = new
            {
                Type = "low_battery",
                Message = $"Mezzo {mezzoId} ha batteria scarica ({batteryLevel}%)",
                MezzoId = mezzoId,
                BatteryLevel = batteryLevel,
                ParkingId = parkingId,
                Priority = "high",
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToAdmins("SystemAlert", notification);
            await _notificationService.SendToParkingMonitors(parkingId, "ParkingUpdate", notification);
            
            _logger.LogWarning("Notified low battery for mezzo {MezzoId}: {BatteryLevel}%", 
                mezzoId, batteryLevel);
        }

        /// <summary>
        /// Notifica malfunzionamento mezzo
        /// </summary>
        public async Task NotifyVehicleError(int mezzoId, string errorMessage, int parkingId)
        {
            var notification = new
            {
                Type = "vehicle_error",
                Message = $"Errore mezzo {mezzoId}: {errorMessage}",
                MezzoId = mezzoId,
                ErrorMessage = errorMessage,
                ParkingId = parkingId,
                Priority = "critical",
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToAdmins("SystemAlert", notification);
            await _notificationService.SendToParkingMonitors(parkingId, "ParkingUpdate", notification);
            
            _logger.LogError("Notified vehicle error for mezzo {MezzoId}: {Error}", 
                mezzoId, errorMessage);
        }

        /// <summary>
        /// Notifica cambio stato parcheggio (occupazione slot)
        /// </summary>
        public async Task NotifyParkingStatusChange(int parkingId, int postiLiberi, int postiOccupati)
        {
            var notification = new
            {
                Type = "parking_status_update",
                ParkingId = parkingId,
                PostiLiberi = postiLiberi,
                PostiOccupati = postiOccupati,
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToParkingMonitors(parkingId, "ParkingUpdate", notification);
            
            // Se parcheggio quasi pieno, notifica amministratori
            if (postiLiberi <= 2)
            {
                var adminNotification = new
                {
                    Type = "parking_almost_full",
                    Message = $"Parcheggio {parkingId} quasi pieno ({postiLiberi} posti liberi)",
                    ParkingId = parkingId,
                    PostiLiberi = postiLiberi,
                    Priority = "medium",
                    Timestamp = DateTime.UtcNow
                };

                await _notificationService.SendToAdmins("SystemAlert", adminNotification);
            }
            
            _logger.LogDebug("Updated parking {ParkingId} status: {Free}/{Total}", 
                parkingId, postiLiberi, postiLiberi + postiOccupati);
        }

        /// <summary>
        /// Notifica ricarica credito utente
        /// </summary>
        public async Task NotifyCreditRecharge(int userId, decimal amount, decimal newBalance)
        {
            var notification = new
            {
                Type = "credit_recharged",
                Message = $"Ricarica di €{amount:F2} completata. Saldo: €{newBalance:F2}",
                Amount = amount,
                NewBalance = newBalance,
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToUser(userId, "CreditNotification", notification);
            _logger.LogInformation("Notified user {UserId} of credit recharge: €{Amount}", 
                userId, amount);
        }

        /// <summary>
        /// Notifica credito insufficiente
        /// </summary>
        public async Task NotifyInsufficientCredit(int userId, decimal requiredAmount, decimal currentBalance)
        {
            var notification = new
            {
                Type = "insufficient_credit",
                Message = $"Credito insufficiente. Richiesti €{requiredAmount:F2}, disponibili €{currentBalance:F2}",
                RequiredAmount = requiredAmount,
                CurrentBalance = currentBalance,
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToUser(userId, "CreditNotification", notification);
            _logger.LogWarning("Notified user {UserId} of insufficient credit", userId);
        }

        /// <summary>
        /// Notifica manutenzione completata su mezzo
        /// </summary>
        public async Task NotifyMaintenanceCompleted(int mezzoId, int parkingId)
        {
            var notification = new
            {
                Type = "maintenance_completed",
                Message = $"Manutenzione completata per mezzo {mezzoId}",
                MezzoId = mezzoId,
                ParkingId = parkingId,
                Timestamp = DateTime.UtcNow
            };

            await _notificationService.SendToAdmins("SystemAlert", notification);
            await _notificationService.SendToParkingMonitors(parkingId, "ParkingUpdate", notification);
            
            _logger.LogInformation("Notified maintenance completion for mezzo {MezzoId}", mezzoId);
        }

        /// <summary>
        /// Broadcast stato generale sistema
        /// </summary>
        public async Task BroadcastSystemStatus(object systemStatus)
        {
            await _notificationService.SendToAll("SystemStatus", systemStatus);
            _logger.LogDebug("Broadcasted system status update");
        }
    }

    /// <summary>
    /// DTO per notifiche generiche
    /// </summary>
    public class NotificationDto
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal"; // low, normal, medium, high, critical
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Data { get; set; } = new();
    }
}