using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Entities
{
    public class PostBlock//文章块
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string PostId { get; set; }
        public Post? Post { get; set; }
        public int Order { get; set; }
        public BlockType Type { get; set; }
        public required string Content { get; set; }
        public string? Caption { get; set; }//描述
    }

}
