using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Domain.Entites
{
    [Table("session_participants")]
    public class SessionParticipant
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("session_id")]
        public Guid SessionId { get; set; }
        [ForeignKey("SessionId")]
        public QuizSession Session { get; set; } = null!;

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Column("total_score")]
        public int TotalScore { get; set; } = 0;

        [Column("answers_count")]
        public int AnswersCount { get; set; } = 0;

        [Column("correct_count")]
        public int CorrectCount { get; set; } = 0;

        [Column("rank")]
        public int? Rank { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}