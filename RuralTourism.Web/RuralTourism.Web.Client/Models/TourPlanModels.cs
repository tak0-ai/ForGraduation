namespace RuralTourism.Web.Client.Models;

public class TourPlanDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string RouteMode { get; set; } = "driving";
    public bool ReturnToStart { get; set; }
    public string StartAddress { get; set; } = string.Empty;
    public List<string> Waypoints { get; set; } = [];
}

public class TourPlanUpsertDto
{
    public string Title { get; set; } = string.Empty;
    public string StartAddress { get; set; } = string.Empty;
    public List<string> Waypoints { get; set; } = [];
    public string RouteMode { get; set; } = "driving";
    public bool ReturnToStart { get; set; }
}
