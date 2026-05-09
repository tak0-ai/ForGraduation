using RuralTourism.Api.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class ChatRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Who sent the request
        public required string RequesterId { get; set; }
        [ForeignKey("RequesterId")]
        public AppUser? Requester { get; set; }

        public ChatRequestType Type { get; set; }

        // Needs to be handled by:
        // For Friend: TargetUser
        // For Group: Group Owner (or Any Admin)
        
        // For Friend Type
        public string? TargetUserId { get; set; }
        [ForeignKey("TargetUserId")]
        public AppUser? TargetUser { get; set; }

        // For GroupJoin Type
        public string? TargetGroupId { get; set; }
        [ForeignKey("TargetGroupId")]
        public ChatRoom? TargetGroup { get; set; }

        public ChatRequestStatus Status { get; set; } = ChatRequestStatus.Pending;
        public string? RequestMessage { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
