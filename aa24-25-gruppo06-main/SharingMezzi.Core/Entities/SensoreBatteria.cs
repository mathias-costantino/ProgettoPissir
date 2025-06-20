namespace SharingMezzi.Core.Entities
{
    public class SensoreBatteria : BaseEntity
    {
        public int MezzoId { get; set; }
        public Mezzo Mezzo { get; set; } = null!;
        
        public int LivelloBatteria { get; set; } // 0-100
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsAttivo { get; set; } = true;
    }
}