namespace RuralTourism.Api.Entities
{
    public class ContentAuditLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? PostId { get; set; }
        public Post? Post { get; set; }
        public string? ResourceId { get; set; }
        public Resource? Resource { get; set; }
        public string? AdminId { get; set; }
        public AppUser? Admin { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
