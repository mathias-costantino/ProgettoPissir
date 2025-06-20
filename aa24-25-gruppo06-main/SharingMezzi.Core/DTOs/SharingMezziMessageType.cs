namespace SharingMezzi.Core.DTOs
{
    /// <summary>
    /// Tipi di messaggio MQTT per comunicazione IoT sistema sharing mezzi
    /// Basato su pattern broker ↔ client per dispositivi sui mezzi
    /// </summary>
    public enum SharingMezziMessageType
    {
        // ===== MESSAGGI SENSORI (Dispositivo → Broker) =====
        BatteryUpdate,           // Aggiornamento livello batteria mezzo
        LockStatusUpdate,        // Stato meccanismo sblocco/blocco
        MovementUpdate,          // Dati GPS/accelerometro
        SlotOccupancyUpdate,     // Stato occupazione slot
        SensorHeartbeat,         // Heartbeat sensori
        
        // ===== COMANDI ATTUATORI (Broker → Dispositivo) =====
        UnlockCommand,           // Comando sblocco mezzo
        LockCommand,             // Comando blocco mezzo  
        LedCommand,              // Comando controllo LED slot
        AlarmCommand,            // Comando allarme anti-furto
        ResetCommand,            // Reset dispositivo
        
        // ===== MESSAGGI SISTEMA =====
        RequestDeviceStatus,     // Richiesta stato dispositivo
        DeviceStatusResponse,    // Risposta stato dispositivo
        CommandAcknowledge,      // Conferma esecuzione comando
        CommandTimeout,          // Timeout comando
        ErrorReport,             // Segnalazione errore
        
        // ===== MESSAGGI GESTIONE =====
        DeviceRegistration,      // Registrazione nuovo dispositivo
        DeviceDeregistration,    // Rimozione dispositivo
        FirmwareUpdate,          // Aggiornamento firmware
        ConfigUpdate,            // Aggiornamento configurazione
        
        // ===== MESSAGGI CORSE =====
        RideStartRequest,        // Richiesta inizio corsa
        RideStartConfirm,        // Conferma inizio corsa
        RideEndRequest,          // Richiesta fine corsa
        RideEndConfirm,          // Conferma fine corsa
    }
}