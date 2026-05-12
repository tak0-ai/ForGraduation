using RuralTourism.Api.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class AppUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // 纯数字ID，不可变更，从1开始递增
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserNo { get; set; }

        public required string UserName { get; set; }
        public string? Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public required string Email { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public DateTime? BannedUntil { get; set; }
        public required byte[] PasswordHash { get; set; }
        public required byte[] PasswordSalt { get; set; }

        // 我的关注
        [InverseProperty("Follower")]
        public List<UserFollow> Following { get; set; } = [];

        // 我的粉丝
        [InverseProperty("Following")]
        public List<UserFollow> Followers { get; set; } = [];
        
        public List<ChatMember> ChatMemberships { get; set; } = new List<ChatMember>();
        public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public List<TourPlan> CreatedTourPlans { get; set; } = new List<TourPlan>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public List<Comment> Comments { get; set; } = [];
    }
}