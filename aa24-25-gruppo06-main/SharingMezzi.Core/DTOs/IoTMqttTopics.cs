namespace SharingMezzi.Core.DTOs
{
    /// <summary>
    /// Topic MQTT per comunicazione IoT sensori/attuatori sui mezzi
    /// Basato su specifica progetto: parking/{id}/mezzi e parking/{id}/stato_mezzi/{id_mezzo}
    /// </summary>
    public static class IoTMqttTopics
    {
        // ===== TOPIC SENSORI (IoT -> Backend) =====
        
        // Sensori batteria su mezzi elettrici  
        public const string SENSOR_BATTERY_DATA = "parking/{0}/sensori/batteria/{1}";        // {0}=parkingId, {1}=mezzoId
        public const string SENSOR_BATTERY_STATUS = "parking/{0}/sensori/batteria/{1}/status"; // Stato sensore
        
        // Sensori meccanici di sblocco/blocco
        public const string SENSOR_LOCK_STATUS = "parking/{0}/sensori/sblocco/{1}";          // Stato meccanismo
        public const string SENSOR_MOVEMENT = "parking/{0}/sensori/movimento/{1}";           // Accelerometro/GPS
        
        // Sensori slot parcheggio
        public const string SENSOR_SLOT_OCCUPANCY = "parking/{0}/sensori/slot/{1}/occupancy"; // {1}=slotId
        public const string SENSOR_SLOT_WEIGHT = "parking/{0}/sensori/slot/{1}/peso";        // Sensore peso
        
        // Sensori ambientali sui mezzi
        public const string SENSOR_TEMPERATURE = "parking/{0}/sensori/temperatura/{1}";      // Temperatura mezzo
        public const string SENSOR_VIBRATION = "parking/{0}/sensori/vibrazione/{1}";         // Anti-furto
        
        // ===== TOPIC COMANDI ATTUATORI (Backend -> IoT) =====
        
        // Comandi meccanismo sblocco/blocco mezzi (COMANDO PRINCIPALE)
        public const string ACTUATOR_LOCK_COMMAND = "parking/{0}/stato_mezzi/{1}";           // Comando principale
        public const string ACTUATOR_LOCK_FEEDBACK = "parking/{0}/stato_mezzi/{1}/feedback"; // Conferma esecuzione
        
        // Comandi LED indicatori su slot
        public const string ACTUATOR_LED_COMMAND = "parking/{0}/attuatori/led/{1}";          // {1}=slotId  
        public const string ACTUATOR_LED_STATUS = "parking/{0}/attuatori/led/{1}/status";    // Stato LED
        
        // Comandi allarme/sirena sui mezzi
        public const string ACTUATOR_ALARM_COMMAND = "parking/{0}/attuatori/allarme/{1}";    // {1}=mezzoId
        public const string ACTUATOR_BUZZER_COMMAND = "parking/{0}/attuatori/buzzer/{1}";    // Segnalatore acustico
        
        // ===== TOPIC SISTEMA E DIAGNOSTICA =====
        
        // Heartbeat dispositivi IoT
        public const string DEVICE_HEARTBEAT = "parking/{0}/dispositivi/{1}/heartbeat";      // {1}=deviceId
        public const string DEVICE_DIAGNOSTIC = "parking/{0}/dispositivi/{1}/diagnostica";
        
        // Topic di sistema generale (come da specifica)
        public const string PARKING_MEZZI_GENERAL = "parking/{0}/mezzi";                     // Stato generale mezzi
        public const string PARKING_STATO_MEZZI_WILDCARD = "parking/{0}/stato_mezzi/#";     // Tutti i comandi mezzi
        
        // ===== WILDCARD SUBSCRIPTIONS =====
        
        public const string ALL_PARKING_SENSORS = "parking/+/sensori/#";                    // Tutti sensori
        public const string ALL_PARKING_ACTUATORS = "parking/+/attuatori/#";               // Tutti attuatori  
        public const string ALL_PARKING_COMMANDS = "parking/+/stato_mezzi/#";              // Tutti comandi
        public const string ALL_DEVICES_HEARTBEAT = "parking/+/dispositivi/+/heartbeat";   // Tutti heartbeat
        
        // Metodi helper per formattare topic
        public static string FormatSensorBattery(int parkingId, int mezzoId) 
            => string.Format(SENSOR_BATTERY_DATA, parkingId, mezzoId);
            
        public static string FormatActuatorLockCommand(int parkingId, int mezzoId)
            => string.Format(ACTUATOR_LOCK_COMMAND, parkingId, mezzoId);
            
        public static string FormatSensorSlotOccupancy(int parkingId, int slotId)
            => string.Format(SENSOR_SLOT_OCCUPANCY, parkingId, slotId);
            
        public static string FormatActuatorLedCommand(int parkingId, int slotId)
            => string.Format(ACTUATOR_LED_COMMAND, parkingId, slotId);
    }
}