using Microsoft.Extensions.Hosting;

namespace RuralTourism.Api.Entities
{
    public class Comment//用户对帖子的评论实体
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string PostId { get; set; }
        public Post? Post { get; set; }

        public required string AuthorId { get; set; }
        public AppUser? Author { get; set; }

        // 用于实现楼中楼回复
        public string? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }

        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Reaction> Reactions { get; set; } = [];
    }

}
