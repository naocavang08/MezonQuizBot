using System.Text.Json;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Domain.Entites
{
    public class Quiz
    {
        public Guid Id { get; set; }

        public Guid CreatorId { get; set; }
        public User Creator { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }
        public QuizCategory? Category { get; set; }

        public JsonDocument Questions { get; set; } = null!;
        public int TotalPoints { get; set; }
        public JsonDocument Settings { get; set; } = null!;

        public QuizVisibility Visibility { get; set; } = QuizVisibility.Private;
        public QuizStatus Status { get; set; } = QuizStatus.Draft;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<QuizSession> Sessions { get; set; } = new List<QuizSession>();
    }
}
