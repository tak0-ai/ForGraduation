namespace RuralTourism.Api.Entities
{
    public class UserMembership
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public required string PlanId { get; set; }
        public MembershipPlan? Plan { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public int BoostCredits { get; set; }
    }
}
