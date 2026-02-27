using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Domain.Entites
{
    [Table("answers")]
    public class Answer
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

        [Required]
        [Column("question_index")]
        public int QuestionIndex { get; set; }

        [Required]
        [Column("selected_option")]
        public int SelectedOption { get; set; }

        [Required]
        [Column("is_correct")]
        public bool IsCorrect { get; set; }

        [Column("points_earned")]
        public int PointsEarned { get; set; } = 0;

        [Column("response_time_ms")]
        public int? ResponseTimeMs { get; set; }

        [Column("answered_at")]
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}
