using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public required string Title { get; set; }
        public string? Body { get; set; }
        public NotificationLevel Level { get; set; } = NotificationLevel.Info;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? TourPlanId { get; set; }
        public TourPlan? TourPlan { get; set; }
        public string? ChatRoomId { get; set; }
        public ChatRoom? ChatRoom { get; set; }
        public string? PostId { get; set; }
        public Post? Post { get; set; }
        public string? TriggerUserId { get; set; }
        public AppUser? TriggerUser { get; set; }
    }
}