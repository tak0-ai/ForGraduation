namespace RuralTourism.Api.Entities;

public class ResourcePhoto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResourceId { get; set; } = null!;
    public Resource? Resource { get; set; }

    public string MediaId { get; set; } = null!;
    public Media? Media { get; set; }

    public string UploaderId { get; set; } = null!;
    public AppUser? Uploader { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}