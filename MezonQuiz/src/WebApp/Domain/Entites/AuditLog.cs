using System.Text.Json;

namespace WebApp.Domain.Entites
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string Action { get; set; } = null!;
        public string? ResourceType { get; set; }
        public Guid? ResourceId { get; set; }

        public JsonDocument? Details { get; set; }
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
