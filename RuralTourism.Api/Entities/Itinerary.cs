namespace RuralTourism.Api.Entities
{
    public class Itinerary//行程
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public required string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPublic { get; set; } = false;
        public List<ItineraryItem> Items { get; set; } = [];
    }

}
