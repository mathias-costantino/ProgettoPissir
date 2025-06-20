namespace SharingMezzi.Core.DTOs
{
    /// <summary>
    /// Messaggio MQTT principale per comunicazione sistema sharing mezzi
    /// Contiene tutti i dati necessari per gestire dispositivi IoT sui mezzi
    /// </summary>
    public class SharingMezziMqttMessage
    {
        // ===== IDENTIFICAZIONE =====
        public SharingMezziMessageType MessageType { get; set; }
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string DeviceId { get; set; } = string.Empty;
        
        // ===== RIFERIMENTI ENTITÀ =====
        public int? MezzoId { get; set; }
        public int? ParcheggioId { get; set; }
        public int? SlotId { get; set; }
        public int? UtenteId { get; set; }
        public int? CorsaId { get; set; }
        
        // ===== DATI BATTERIA =====
        public int? BatteryLevel { get; set; }                 // 0-100%
        public float? Voltage { get; set; }
        public float? Current { get; set; }
        public float? Temperature { get; set; }
        public bool? IsCharging { get; set; }
        
        // ===== DATI POSIZIONE =====
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public float? Speed { get; set; }
        public float? Acceleration { get; set; }
        public bool? IsMoving { get; set; }
        
        // ===== STATO MECCANISMO =====
        public string LockState { get; set; } = "locked";      // "locked", "unlocked", "error"
        public bool? IsSecure { get; set; }
        public DateTime? LastOperation { get; set; }
        
        // ===== COMANDI =====
        public string Command { get; set; } = string.Empty;    // "unlock", "lock", "reset", etc.
        public string Priority { get; set; } = "normal";       // "low", "normal", "high", "emergency"
        public int TimeoutSeconds { get; set; } = 30;
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        // ===== FEEDBACK COMANDI =====
        public string ExecutionStatus { get; set; } = string.Empty; // "success", "error", "timeout"
        public string StatusMessage { get; set; } = string.Empty;
        public float? ExecutionTime { get; set; }               // millisecondi
        
        // ===== DATI SLOT =====
        public bool? IsSlotOccupied { get; set; }
        public float? SlotWeight { get; set; }
        public string LedColor { get; set; } = "green";
        public string LedPattern { get; set; } = "solid";
        
        // ===== DATI CORSA =====
        public DateTime? RideStartTime { get; set; }
        public DateTime? RideEndTime { get; set; }
        public decimal? RideCost { get; set; }
        public int? RideDurationMinutes { get; set; }
        
        // ===== DIAGNOSTICA =====
        public bool IsOnline { get; set; } = true;
        public float? SignalStrength { get; set; }
        public string FirmwareVersion { get; set; } = "1.0.0";
        public float? UptimeHours { get; set; }
        public List<string> Errors { get; set; } = new();
        
        // ===== METADATI =====
        public object? AdditionalData { get; set; }
        public string SourceTopic { get; set; } = string.Empty;
        public string TargetTopic { get; set; } = string.Empty;
    }
}