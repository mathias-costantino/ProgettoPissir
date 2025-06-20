namespace SharingMezzi.Core.DTOs
{
    public class BatteryStatusDto
    {
        public int MezzoId { get; set; }
        public int BatteryLevel { get; set; }
        public DateTime Timestamp { get; set; }
        public int ParkingId { get; set; }
        public bool IsCharging { get; set; } = false;
        public string Status { get; set; } = "OK";
    }

    public class BatteryRequestDto
    {
        public int MezzoId { get; set; }
        public int ParkingId { get; set; }
        public DateTime Timestamp { get; set; }
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }

    public class AvviaCorseResponseDto
    {
        public int CorsaId { get; set; }
        public bool MezzoSbloccato { get; set; }
        public string Message { get; set; } = "";
        public int? BatteryLevel { get; set; }
        public bool RequiredBatteryCheck { get; set; }
    }

    public class TestSequenceResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? CorsaId { get; set; }
        public bool TestData { get; set; }
        public int? BatteryLevel { get; set; }
        public string? MezzoTipo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
