using RuralTourism.Api.Enums;

namespace RuralTourism.Api.DTOs
{
    public class PostBlockDto
    {
        public string? Id { get; set; }
        public int Order { get; set; }
        public BlockType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }
}
