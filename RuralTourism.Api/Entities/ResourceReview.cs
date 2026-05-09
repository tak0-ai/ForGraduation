namespace RuralTourism.Api.Entities;

public class ResourceReview
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResourceId { get; set; } = null!;
    public Resource? Resource { get; set; }

    public string UserId { get; set; } = null!;
    public AppUser? User { get; set; }

    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
