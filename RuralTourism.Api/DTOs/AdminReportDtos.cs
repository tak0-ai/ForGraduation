namespace RuralTourism.Api.DTOs;

public class AdminReportSummaryDto
{
    public int Posts { get; set; }
    public int Villages { get; set; }
    public int Attractions { get; set; }
    public int TotalViews { get; set; }
    public int TotalInteractions { get; set; }
    public double TotalHeat { get; set; }
}

public class AdminPostReportDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Views { get; set; }
    public int Comments { get; set; }
    public int Likes { get; set; }
    public int Bookmarks { get; set; }
    public int Shares { get; set; }
    public double HeatScore { get; set; }
}

public class AdminVillageReportDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Views { get; set; }
    public int Reviews { get; set; }
    public int ChildResourceViews { get; set; }
    public int ChildResourceReviews { get; set; }
    public int Shares { get; set; }
    public double HeatScore { get; set; }
}

public class AdminAttractionReportDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Views { get; set; }
    public int Reviews { get; set; }
    public int Shares { get; set; }
    public double HeatScore { get; set; }
}

public class AdminReportOverviewDto
{
    public AdminReportSummaryDto Summary { get; set; } = new();
    public List<AdminPostReportDto> Posts { get; set; } = [];
    public List<AdminVillageReportDto> Villages { get; set; } = [];
    public List<AdminAttractionReportDto> Attractions { get; set; } = [];
}
