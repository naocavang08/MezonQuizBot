namespace WebApp.Domain.Entites
{
    public class SessionParticipant
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }
        public QuizSession Session { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public int TotalScore { get; set; }
        public int AnswersCount { get; set; }
        public int CorrectCount { get; set; }
        public int? Rank { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}