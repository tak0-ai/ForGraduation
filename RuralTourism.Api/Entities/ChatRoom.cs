using RuralTourism.Api.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class ChatRoom
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Group Number for Short ID search
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoomNo { get; set; }

        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool IsGroup { get; set; }
        public bool IsLocked { get; set; }
        public bool IsArchived { get; set; } // For dismissed groups
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedById { get; set; }
        public AppUser? CreatedBy { get; set; }
        public string? CoverMediaId { get; set; }
        public Media? CoverMedia { get; set; }
        public string? TravelPlanId { get; set; }
        public TourPlan? TravelPlan { get; set; }
        public List<ChatMember> Members { get; set; } = [];
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
