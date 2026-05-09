namespace RuralTourism.Api.Entities
{
    public class MembershipPlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal PricePerMonth { get; set; }
        public double ExposureMultiplier { get; set; } = 1;
        public int PriorityLevel { get; set; }
        public List<UserMembership> Subscriptions { get; set; } = [];
    }
}
