using System.ComponentModel.DataAnnotations;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.Dtos
{
    public class QuizDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CreatorId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }

        [Required]
        public List<QuizQuestion> Questions { get; set; } = new();

        public int TotalPoints { get; set; } = 0;

        public QuizSettings Settings { get; set; } = new();

        public QuizVisibility Visibility { get; set; }

        public QuizStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class ListQuizDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
    }
}
