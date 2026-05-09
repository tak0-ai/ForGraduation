namespace RuralTourism.Api.Entities
{
    public class Attraction : Resource
    {
        public string? OpeningHours { get; set; } // "08:00-17:00"
        public string? TicketPrice { get; set; } // "成人票:80元, 学生票:40元"
        public string? BestVisitTime { get; set; } // "春季,秋季"
    }

}
