namespace RuralTourism.Api.Entities
{
    public class ItineraryItem//单个行程项
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string ItineraryId { get; set; }
        public Itinerary? Itinerary { get; set; }
        public required string ResourceId { get; set; } // 关联的资源ID
        public int DayNumber { get; set; } // 行程的第几天
        public TimeSpan? StartTime { get; set; }
        public string? Notes { get; set; } // 用户备注
    }

}
