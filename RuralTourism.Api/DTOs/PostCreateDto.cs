namespace RuralTourism.Api.DTOs
{
    public class PostCreateDto
    {
        public string? Title { get; set; }
        public bool IsDraft { get; set; } = true;
        
        public string? CoverMediaId { get; set; }
        public List<PostBlockDto> Blocks { get; set; } = new();
    }
}
