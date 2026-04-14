using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using WebApp.Application.AuditLog.Dtos;

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

        [Required]
        [Column("details", TypeName = "jsonb")]
        public AuditDetailsDto Details { get; set; } = new();
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
