using static WebApp.Domain.Enums.Status;

namespace WebApp.Domain.Entites
{
    public class QuizSession
    {
        public Guid Id { get; set; }

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public Guid HostId { get; set; }
        public User Host { get; set; } = null!;

        public SessionStatus Status { get; set; } = SessionStatus.Waiting;
        public int CurrentQuestion { get; set; }

        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? MezonChannelId { get; set; }

        public int? MaxParticipants { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<SessionParticipant> Participants { get; set; } = new List<SessionParticipant>();
    }
}
