using System.ComponentModel.DataAnnotations;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.ManageQuiz.Dtos
{
    public class SaveQuizDto
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }

        [Required]
        public List<QuizQuestion> Questions { get; set; } = new();

        public QuizSettings Settings { get; set; } = new();

        public QuizVisibility Visibility { get; set; }

        public QuizStatus Status { get; set; }
    }
}
