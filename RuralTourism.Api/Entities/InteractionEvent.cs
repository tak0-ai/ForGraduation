using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class InteractionEvent//用户的交互事件存储
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public required string ResourceId { get; set; } // 关联的资源ID (景点、美食等)
        public InteractionEventType EventType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Metadata { get; set; } // JSON格式，存储额外信息，如推荐来源
    }
}
