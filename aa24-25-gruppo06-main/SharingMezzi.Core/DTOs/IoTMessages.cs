namespace SharingMezzi.Core.DTOs
{
    public class SensorBatteryMessage
    {
        public int MezzoId { get; set; }
        public int BatteryLevel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UnlockCommand
    {
        public int MezzoId { get; set; }
        public int SlotId { get; set; }
        public string Action { get; set; } = "unlock"; // unlock, lock
        public DateTime Timestamp { get; set; }
    }

    public class StatusUpdate
    {
        public int MezzoId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}