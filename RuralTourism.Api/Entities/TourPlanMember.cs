using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class TourPlanMember
    {
        public required string TourPlanId { get; set; }
        public TourPlan? TourPlan { get; set; }
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public TourPlanMemberStatus Status { get; set; } = TourPlanMemberStatus.Pending;
        public bool IsLeader { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExitedAt { get; set; }
    }
}
