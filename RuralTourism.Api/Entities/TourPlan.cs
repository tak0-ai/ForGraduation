using RuralTourism.Api.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class TourPlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required string CreatedById { get; set; }
        public AppUser? CreatedBy { get; set; }
        public TourPlanStatus Status { get; set; } = TourPlanStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsGroupPlan { get; set; }
        public bool IsLocked { get; set; }
        public string? AutoRouteData { get; set; }

        [NotMapped]
        public string? ChatRoomId { get; set; }

        public ChatRoom? ChatRoom { get; set; }
        public List<TourPlanMember> Members { get; set; } = [];
        public List<TourPlanWaypoint> Waypoints { get; set; } = [];
        public DateTime? CompletedAt { get; set; }
    }
}
