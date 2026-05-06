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

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                throw new ArgumentException("Quiz title is required.", nameof(Title));
            }

            if (Title.Trim().Length > 500)
            {
                throw new ArgumentException("Quiz title must not exceed 500 characters.", nameof(Title));
            }

            if (Questions is null)
            {
                throw new ArgumentException("Questions are required.", nameof(Questions));
            }

            if (Settings is null)
            {
                throw new ArgumentException("Quiz settings are required.", nameof(Settings));
            }

            Settings.Validate();

            foreach (var question in Questions)
            {
                if (question is null)
                {
                    throw new ArgumentException("Question payload is invalid.", nameof(Questions));
                }

                question.Validate();
            }
        }
    }
}
