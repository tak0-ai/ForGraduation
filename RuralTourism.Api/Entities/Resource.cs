namespace RuralTourism.Api.Entities
{
    public class Resource
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double Latitude { get; set; } // 纬度
        public double Longitude { get; set; } // 经度
        public double AverageRating { get; set; } = 5;
        public string? Tags { get; set; } // 标签，用逗号分隔，如 "古镇,小桥流水,美食"
        public string? CoverMediaId { get; set; }
        public Media? CoverMedia { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
