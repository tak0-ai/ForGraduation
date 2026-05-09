namespace RuralTourism.Api.Entities;

public class UserWallMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TargetUserId { get; set; } = null!;
    public AppUser? TargetUser { get; set; }

    public string SenderUserId { get; set; } = null!;
    public AppUser? SenderUser { get; set; }

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
