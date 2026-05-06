using System.ComponentModel.DataAnnotations;
using WebApp.Domain.Entites;

namespace WebApp.Application.ManageQuiz.Dtos
{
    public class QuizQuestion
    {
        public int Id { get; set; }

        public int Index { get; set; }

        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống")]
        public string Content { get; set; } = null!;
        public string? MediaUrl { get; set; }
        [Range(10, 30, ErrorMessage = "Thời gian giới hạn phải từ 10 đến 30 giây")]
        public int TimeLimitSeconds { get; set; } = 30;
        [Range(1, 20, ErrorMessage = "Điểm phải từ 1 đến 20")]
        public int Points { get; set; } = 10;

        public QuestionType QuestionType { get; set; }
        public List<QuizOption> Options { get; set; } = new();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Content))
                throw new ArgumentException("Question content is required.", nameof(Content));

            if (TimeLimitSeconds < 10 || TimeLimitSeconds > 30)
                throw new ArgumentException("Time limit must be between 10 and 30 seconds.", nameof(TimeLimitSeconds));

            if (Points < 1 || Points > 20)
                throw new ArgumentException("Points must be between 1 and 20.", nameof(Points));

            if (Options is null)
                throw new ArgumentException("Question options are required.", nameof(Options));

            foreach (var option in Options)
            {
                if (option is null)
                {
                    throw new ArgumentException("Question option is invalid.", nameof(Options));
                }

                option.Validate();
            }

            var correctCount = Options.Count(o => o.IsCorrect);

            if (QuestionType == QuestionType.TrueFalse)
            {
                if (Options.Count != 2 || correctCount != 1)
                {
                    throw new ArgumentException("True/False questions must have exactly 2 options and 1 correct answer.", nameof(Options));
                }

                return;
            }

            if (Options.Count < 2)
                throw new ArgumentException("Questions must have at least 2 options.", nameof(Options));

            if (QuestionType == QuestionType.SingleChoice && correctCount != 1)
                throw new ArgumentException("Single choice questions must have exactly 1 correct answer.", nameof(Options));

            if (QuestionType == QuestionType.MultipleChoice && correctCount < 2)
                throw new ArgumentException("Multiple choice questions must have at least 2 correct answers.", nameof(Options));
        }
    }

    public class QuizOption
    {
        public int Id { get; set; }
        public int Index { get; set; }
        [Required(ErrorMessage = "Nội dung đáp án không được để trống")]
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                throw new ArgumentException("Option content is required.", nameof(Content));
            }
        }
    }

    public enum QuestionType
    {
        SingleChoice = 0,
        MultipleChoice = 1,
        TrueFalse = 2
    }
}
