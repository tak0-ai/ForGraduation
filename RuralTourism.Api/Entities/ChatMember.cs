using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class ChatMember
    {
        public required string ChatRoomId { get; set; }
        public ChatRoom? ChatRoom { get; set; }
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public ChatMemberRole Role { get; set; } = ChatMemberRole.Member;
        public bool CanEditPlan { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? MuteUntil { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
