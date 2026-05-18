using System.ComponentModel.DataAnnotations.Schema;

namespace RuralTourism.Api.Entities
{
    public class OperationLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = "Anonymous";

        public string ActionName { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public string? RequestPayload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}