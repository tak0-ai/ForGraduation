namespace RuralTourism.Api.Entities
{
    public class UserFollow
    {
        // 关注者的ID 
        public required string FollowerId { get; set; }
        public AppUser? Follower { get; set; }

        // 被关注者的ID 
        public required string FollowingId { get; set; }
        public AppUser? Following { get; set; }

        // 关注发生的时间
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
