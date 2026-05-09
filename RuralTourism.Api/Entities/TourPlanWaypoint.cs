namespace RuralTourism.Api.Entities
{
    public class TourPlanWaypoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string TourPlanId { get; set; }
        public TourPlan? TourPlan { get; set; }
        public required string ResourceId { get; set; }
        public Resource? Resource { get; set; }
        public int Sequence { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
