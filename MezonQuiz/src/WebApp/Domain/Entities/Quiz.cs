using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using WebApp.Application.Dtos;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Domain.Entites
{
    [Table("quizzes")]
    public class Quiz
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("creator_id")]
        public Guid CreatorId { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(500)]
        public string Title { get; set; } = null!;

        [Column("description")]
        public string? Description { get; set; }

        [Column("category_id")]
        public Guid? CategoryId { get; set; }

        [Required]
        [Column("questions", TypeName = "jsonb")]
        public List<QuizQuestion> Questions { get; set; } = new();

        [Column("total_points")]
        public int TotalPoints { get; set; } = 0;

        [Column("settings", TypeName = "jsonb")]
        public QuizSettings Settings { get; set; } = new();

        [Column("quiz_visibility")]
        public QuizVisibility Visibility { get; set; } = QuizVisibility.Private;

        [Column("quiz_status")]
        public QuizStatus Status { get; set; } = QuizStatus.Draft;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CreatorId")]
        public User Creator { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public QuizCategory? Category { get; set; }
    }
}
