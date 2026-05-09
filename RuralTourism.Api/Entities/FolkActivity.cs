namespace RuralTourism.Api.Entities
{
    public class FolkActivity : Resource
    {
        public DateTime? EventDate { get; set; } // 活动具体日期
        public string? Duration { get; set; } // 例如 "每年4月-5月" 或 "2小时"
        public string? Organizer { get; set; } // 主办方
    }
}
