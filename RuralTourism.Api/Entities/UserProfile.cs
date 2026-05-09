using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class UserProfile//用户资料
    {
        [Key, ForeignKey("User")]
        public string UserId { get; set; } = default!;
        public AppUser User { get; set; } = default!;
        public string? Gender { get; set; } // "男", "女", "保密"
        public string? AgeRange { get; set; } // "90后", "00后"
        public string? HomeCity { get; set; }
        public string? TravelStyle { get; set; } // "家庭出游", "独自探险"
        public string? InterestTags { get; set; } // "历史古迹,自然风光,美食控"
    }
}
