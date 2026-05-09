using RuralTourism.Api.Enums;

namespace RuralTourism.Api.DTOs;

public class RecommendedPostDto
{
    public string Id { get; set; } = null!;
    public string? Title { get; set; }
    public string? CoverMediaId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public PostStatus Status { get; set; }
}

public class RecommendedResourceDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public double AverageRating { get; set; }
    public string? CoverMediaId { get; set; }
    public string ResourceType { get; set; } = null!;
}

public class PopularBeautifulVillageDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? CoverMediaId { get; set; }
    public double AverageRating { get; set; }
    public double HeatScore { get; set; }
    public double TotalScore { get; set; }
}

public class SearchRecommendedPostDto : RecommendedPostDto
{
    public double Score { get; set; }
}

public class SearchRecommendedResourceDto : RecommendedResourceDto
{
    public double Score { get; set; }
}
