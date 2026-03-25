using static WebApp.Domain.Enums.Status;
using WebApp.Application.ManageQuiz.Dtos;

namespace WebApp.Application.ManageQuizSession.Dtos
{
    public class SessionStateChangedDto
    {
        public Guid SessionId { get; set; }
        public SessionStatus Status { get; set; }
        public int CurrentQuestion { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class QuizSessionQuestionDto
    {
        public Guid SessionId { get; set; }
        public int QuestionIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }
        public QuestionType QuestionType { get; set; }
        public List<QuizSessionQuestionOptionDto> Options { get; set; } = new();
    }

    public class QuizSessionQuestionOptionDto
    {
        public int Index { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
