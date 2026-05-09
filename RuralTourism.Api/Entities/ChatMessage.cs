using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string ChatRoomId { get; set; }
        public ChatRoom? ChatRoom { get; set; }
        public required string AuthorId { get; set; }
        public AppUser? Author { get; set; }
        public ChatMessageType Type { get; set; } = ChatMessageType.Text;
        public required string Content { get; set; }
        public string? MediaId { get; set; }
        public Media? Media { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsPinned { get; set; }
        public string? ReferenceId { get; set; }
    }
}
