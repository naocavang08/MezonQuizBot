using static WebApp.Domain.Enums.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Domain.Entites
{
    [Table("quiz_sessions")]
    public class QuizSession
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("quiz_id")]
        public Guid QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = null!;

        [Required]
        [Column("host_id")]
        public Guid HostId { get; set; }
        [ForeignKey("HostId")]
        public User Host { get; set; } = null!;

        [Column("session_status")]
        public SessionStatus Status { get; set; } = SessionStatus.Waiting;

        [Column("current_question")]
        public int CurrentQuestion { get; set; } = 0;

        [Column("deep_link")]
        public string? DeepLink { get; set; }

        [Column("qr_code_url")]
        public string? QrCodeUrl { get; set; }

        [Column("mezon_channel_id")]
        [MaxLength(255)]
        public string? MezonChannelId { get; set; }

        [Column("max_participants")]
        public int? MaxParticipants { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("finished_at")]
        public DateTime? FinishedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
