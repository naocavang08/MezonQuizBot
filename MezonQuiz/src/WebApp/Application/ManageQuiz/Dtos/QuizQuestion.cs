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

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Content))
                return false;

            if (TimeLimitSeconds < 10 || TimeLimitSeconds > 30)
                return false;

            if (Points < 1 || Points > 20)
                return false;

            if (Options.Any(o => string.IsNullOrWhiteSpace(o.Content)))
                return false;

            var correctCount = Options.Count(o => o.IsCorrect);

            if (QuestionType == QuestionType.TrueFalse)
                return Options.Count == 2 && correctCount == 1;

            if (Options.Count < 2)
                return false;

            if (QuestionType == QuestionType.SingleChoice && correctCount != 1)
                return false;

            if (QuestionType == QuestionType.MultipleChoice && correctCount < 2)
                return false;

            return true;
        }
    }

    public class QuizOption
    {
        public int Id { get; set; }
        public int Index { get; set; }
        [Required(ErrorMessage = "Nội dung đáp án không được để trống")]
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;
    }

    public enum QuestionType
    {
        SingleChoice = 0,
        MultipleChoice = 1,
        TrueFalse = 2
    }
}