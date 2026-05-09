using RuralTourism.Api.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class Booking
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public AppUser? User { get; set; }
        public required string ResourceId { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public int NumberOfGuests { get; set; } = 1;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public string? ContactInfo { get; set; } // JSON, 如 {"name":"张三", "phone":"138..."}
    }
}
