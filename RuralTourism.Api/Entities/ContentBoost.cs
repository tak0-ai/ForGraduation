namespace RuralTourism.Api.Entities
{
    public class ContentBoost
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string OwnerId { get; set; }
        public AppUser? Owner { get; set; }
        public string? PostId { get; set; }
        public Post? Post { get; set; }
        public string? ResourceId { get; set; }
        public Resource? Resource { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public double BoostMultiplier { get; set; } = 1.5;
    }
}
