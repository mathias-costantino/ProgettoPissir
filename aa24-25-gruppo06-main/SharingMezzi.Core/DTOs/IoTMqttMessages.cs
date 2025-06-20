// SharingMezzi.Core/DTOs/IoTMqttMessages.cs
using System.ComponentModel.DataAnnotations;

namespace SharingMezzi.Core.DTOs
{
    // ===== MESSAGGI SENSORI =====

    /// <summary>
    /// Dati sensore batteria da mezzo elettrico
    /// Inviato ogni 30-60 secondi + eventi di variazione
    /// </summary>
    public class MezzoBatteryData
    {
        [Required]
        public int MezzoId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        [Range(0, 100)]
        public int BatteryLevel { get; set; }        // 0-100%
        
        public float Voltage { get; set; }           // Voltaggio batteria
        public float Current { get; set; }           // Corrente (+ = carica, - = scarica)
        public float Temperature { get; set; }       // Temperatura batteria °C
        public bool IsCharging { get; set; }         // In carica
        public bool IsHealthy { get; set; } = true;  // Stato salute batteria
        public int CycleCount { get; set; }          // Cicli di carica/scarica
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SensorId { get; set; } = string.Empty;  // ID fisico sensore
        
        // Metadati aggiuntivi
        public string FirmwareVersion { get; set; } = "1.0.0";
        public float SignalStrength { get; set; } = -50.0f;  // dBm
    }

    /// <summary>
    /// Stato meccanismo sblocco/blocco mezzo
    /// Inviato quando cambia stato + heartbeat ogni 5 min
    /// </summary>
    public class MezzoLockStatus
    {
        [Required]
        public int MezzoId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        [Required]
        public string LockState { get; set; } = "locked";  // "locked", "unlocked", "error", "jammed"
        
        public bool IsSecure { get; set; } = true;         // Meccanismo sicuro
        public float LockStrength { get; set; }            // Forza serratura (Nm)
        public int UnlockAttempts { get; set; }            // Tentativi sblocco falliti
        public DateTime LastOperation { get; set; } = DateTime.UtcNow; // Ultimo comando ricevuto
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ActuatorId { get; set; } = string.Empty;
        
        // Dati diagnostici
        public bool RequiresMaintenance { get; set; } = false;
        public string ErrorCode { get; set; } = string.Empty;
        public int? CorsaId { get; set; }  // Corsa attiva se sbloccato
    }

    /// <summary>
    /// Dati movimento/posizione mezzo (GPS + accelerometro)
    /// Inviato quando in movimento + posizione ogni 2 min
    /// </summary>
    public class MezzoMovementData
    {
        [Required]
        public int MezzoId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        [Range(-90, 90)]
        public double Latitude { get; set; }
        
        [Range(-180, 180)]
        public double Longitude { get; set; }
        
        [Range(0, 100)]
        public float Speed { get; set; }                   // km/h
        
        public float Acceleration { get; set; }            // m/s²
        public bool IsMoving { get; set; }
        public bool IsVibrating { get; set; }              // Anti-furto
        public float Distance { get; set; }                // Distanza dal parcheggio (m)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string GpsAccuracy { get; set; } = "high";  // "high", "medium", "low"
        
        // Dati aggiuntivi movimento
        public float Direction { get; set; }               // Direzione in gradi (0-360)
        public float Altitude { get; set; }                // Altitudine (m)
        public int SatelliteCount { get; set; }            // Numero satelliti GPS
    }

    /// <summary>
    /// Stato occupancy slot parcheggio
    /// Inviato quando cambia stato + heartbeat ogni 10 min
    /// </summary>
    public class SlotOccupancyData
    {
        [Required]
        public int SlotId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        public bool IsOccupied { get; set; }
        public int? MezzoId { get; set; }                  // Mezzo presente (se rilevabile)
        public float Weight { get; set; }                  // Peso rilevato (kg)
        public bool SensorWorking { get; set; } = true;    // Stato sensore
        public DateTime LastChange { get; set; } = DateTime.UtcNow; // Ultimo cambio stato
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SensorType { get; set; } = "weight"; // "weight", "magnetic", "optical"
        
        // Dati aggiuntivi slot
        public float SensorVoltage { get; set; } = 3.3f;   // Voltaggio sensore
        public bool LedWorking { get; set; } = true;       // Stato LED
        public string SlotCondition { get; set; } = "good"; // "good", "damaged", "maintenance"
    }

    // ===== MESSAGGI COMANDI ATTUATORI =====

    /// <summary>
    /// Comando sblocco/blocco mezzo (MESSAGGIO PRINCIPALE)
    /// Inviato dal backend quando utente inizia/termina corsa
    /// </summary>
    public class MezzoLockCommand
    {
        [Required]
        public string Comando { get; set; } = string.Empty;  // "sblocca", "blocca", "reset", "test"
        
        [Required]
        public int MezzoId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        public int? UtenteId { get; set; }                    // Chi ha richiesto comando
        public int? CorsaId { get; set; }                     // Corsa associata
        
        [Range(5, 300)]
        public int TimeoutSeconds { get; set; } = 30;         // Timeout esecuzione
        
        public bool ForceUnlock { get; set; } = false;        // Sblocco forzato (emergenza)
        public string Priority { get; set; } = "normal";      // "low", "normal", "high", "emergency"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CommandId { get; set; } = Guid.NewGuid().ToString(); // ID per tracking

        // Parametri aggiuntivi
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        // Metadati comando
        public string RequestSource { get; set; } = "api";    // "api", "admin", "emergency"
        public bool RequireConfirmation { get; set; } = true; // Richiede conferma esecuzione
    }

    /// <summary>
    /// Feedback esecuzione comando attuatore
    /// Inviato dal dispositivo IoT per confermare esecuzione
    /// </summary>
    public class ActuatorFeedback
    {
        [Required]
        public string CommandId { get; set; } = string.Empty;  // Riferimento comando originale
        
        [Required]
        public int MezzoId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        [Required]
        public string Status { get; set; } = string.Empty;     // "success", "error", "timeout", "partial"
        
        public string Message { get; set; } = string.Empty;    // Dettagli esecuzione
        public float ExecutionTime { get; set; }               // Tempo esecuzione (ms)
        public bool RequiresRetry { get; set; } = false;       // Richiede nuovo tentativo
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Dati specifici risultato
        public Dictionary<string, object> ResultData { get; set; } = new();
        
        // Diagnostica aggiuntiva
        public string ErrorCode { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public string DeviceStatus { get; set; } = "operational"; // "operational", "warning", "error"
    }

    /// <summary>
    /// Comando controllo LED slot
    /// Per indicare stato disponibilità/occupazione
    /// </summary>
    public class SlotLedCommand
    {
        [Required]
        public int SlotId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        [Required]
        public string Color { get; set; } = "green";        // "green"=libero, "red"=occupato, "yellow"=manutenzione, "blue"=riservato, "off"=spento
        
        public string Pattern { get; set; } = "solid";      // "solid", "blink", "pulse", "fade", "strobe"
        public int Brightness { get; set; } = 100;          // 0-100% luminosità
        public int? DurationSeconds { get; set; }           // Durata comando (null = indefinito)
        public float BlinkRate { get; set; } = 1.0f;        // Frequenza lampeggio (Hz)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CommandId { get; set; } = Guid.NewGuid().ToString();
        
        // Parametri avanzati LED
        public string Priority { get; set; } = "normal";    // "low", "normal", "high", "emergency"
        public bool OverrideCurrent { get; set; } = false;  // Sovrascrive comando attuale
        public Dictionary<string, object> CustomParams { get; set; } = new();
    }

    /// <summary>
    /// Comando allarme/sirena per mezzi
    /// Utilizzato per anti-furto o emergenze
    /// </summary>
    public class MezzoAlarmCommand
    {
        [Required]
        public int MezzoId { get; set; }
        
        [Required]
        public int ParkingId { get; set; }
        
        [Required]
        public string Action { get; set; } = "activate";    // "activate", "deactivate", "test"
        
        public string AlarmType { get; set; } = "anti_theft"; // "anti_theft", "emergency", "maintenance"
        public int DurationSeconds { get; set; } = 30;       // Durata allarme
        public int Volume { get; set; } = 80;                // Volume 0-100%
        public string Pattern { get; set; } = "intermittent"; // "continuous", "intermittent", "pulse"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CommandId { get; set; } = Guid.NewGuid().ToString();
        
        // Metadati allarme
        public string TriggerReason { get; set; } = string.Empty; // Motivo attivazione
        public int? UtenteId { get; set; }                        // Utente che ha attivato
        public bool NotifyPolice { get; set; } = false;           // Notifica forze dell'ordine
    }

    /// <summary>
    /// Heartbeat dispositivo IoT
    /// Inviato periodicamente per monitorare stato dispositivi
    /// </summary>
    public class DeviceHeartbeat
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;
        
        public int? MezzoId { get; set; }                      // Se dispositivo montato su mezzo
        public int? SlotId { get; set; }                       // Se dispositivo slot
        
        [Required]
        public int ParkingId { get; set; }
        
        [Required]
        public string DeviceType { get; set; } = string.Empty; // "battery_sensor", "lock_actuator", "led", "gps"
        
        public bool IsOnline { get; set; } = true;
        public float SignalStrength { get; set; } = -50.0f;    // dBm
        public string FirmwareVersion { get; set; } = "1.0.0";
        public float UptimeHours { get; set; }
        public DateTime LastReboot { get; set; } = DateTime.UtcNow;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Stato hardware
        public float CpuUsage { get; set; }                    // % utilizzo CPU
        public float MemoryUsage { get; set; }                 // % utilizzo memoria
        public float Temperature { get; set; }                 // Temperatura dispositivo °C
        public float BatteryVoltage { get; set; } = 3.3f;      // Voltaggio alimentazione
        
        // Errori e warning
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool RequiresMaintenance { get; set; } = false;
    }

    /// <summary>
    /// Comando configurazione dispositivo
    /// Per aggiornare parametri operativi dei dispositivi IoT
    /// </summary>
    public class DeviceConfigCommand
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;
        
        [Required]
        public int ParkingId { get; set; }
        
        [Required]
        public Dictionary<string, object> ConfigParameters { get; set; } = new();
        
        public bool ApplyImmediately { get; set; } = true;
        public bool PersistConfig { get; set; } = true;        // Salva configurazione in memoria persistente
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CommandId { get; set; } = Guid.NewGuid().ToString();
        
        // Metadati configurazione
        public string ConfigVersion { get; set; } = "1.0";
        public string UpdatedBy { get; set; } = "system";      // Chi ha fatto l'aggiornamento
        public bool RequireReboot { get; set; } = false;       // Richiede riavvio dispositivo
    }
}