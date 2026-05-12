using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class TourPlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Title { get; set; }
        public required string CreatedById { get; set; }
        public AppUser? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string RouteMode { get; set; } = "driving";
        public bool ReturnToStart { get; set; }
        public string StartAddress { get; set; } = string.Empty;
        public string? WaypointsJson { get; set; }
        
        [NotMapped]
        public List<string> Waypoints
        {
            get
            {
                if (string.IsNullOrEmpty(WaypointsJson)) return [];
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(WaypointsJson) ?? [];
                }
                catch (JsonException)
                {
                    return [];
                }
            }
            set => WaypointsJson = JsonSerializer.Serialize(value);
        }
    }
}