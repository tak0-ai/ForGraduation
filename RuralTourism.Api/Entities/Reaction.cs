using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class Reaction//用户对帖子的反应实体
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Target can be a Post OR a Comment
        public string? PostId { get; set; }
        public Post? Post { get; set; }

        public string? CommentId { get; set; }
        public Comment? Comment { get; set; }

        public required string UserId { get; set; }
        public AppUser? User { get; set; }

        public ReactionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
