namespace WebApp.Domain.Entites
{
    public class Answer
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }
        public QuizSession Session { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public int QuestionIndex { get; set; }
        public int SelectedOption { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public int? ResponseTimeMs { get; set; }

        public DateTime AnsweredAt { get; set; }
    }
}
