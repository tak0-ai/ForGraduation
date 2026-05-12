using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class Post//帖子实体
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string AuthorId { get; set; }
        public AppUser? Author { get; set; }

        public string? Title { get; set; }

        public string? CoverMediaId { get; set; }
        public Media? CoverMedia { get; set; }
        public PostStatus Status { get; set; } = PostStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsHidden { get; set; }
        public bool IsDeleted { get; set; }
        public string? HiddenReason { get; set; }
        public DateTime? HiddenAt { get; set; }
        public string? HiddenById { get; set; }
        public AppUser? HiddenBy { get; set; }
        public bool IsFeatured { get; set; }
        public string? RecommendationReason { get; set; }
        public List<PostBlock> Blocks { get; set; } = [];
        public List<Comment> Comments { get; set; } = [];
        public List<Reaction> Reactions { get; set; } = [];
    }

}