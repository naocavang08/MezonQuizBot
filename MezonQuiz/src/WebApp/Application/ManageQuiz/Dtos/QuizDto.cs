using System.ComponentModel.DataAnnotations;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.ManageQuiz.Dtos
{
    public class QuizDto
    {
        public Guid Id { get; set; }

        public Guid CreatorId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }

        public int TotalPoints { get; set; } = 0;

        public QuizVisibility Visibility { get; set; }

        public QuizStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
