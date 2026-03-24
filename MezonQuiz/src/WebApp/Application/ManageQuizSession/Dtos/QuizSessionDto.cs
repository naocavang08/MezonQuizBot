using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.ManageQuizSession.Dtos
{
    public class CreateQuizSessionDto
    {
        public Guid QuizId { get; set; }
        public int? MaxParticipants { get; set; }
        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? MezonChannelId { get; set; }
    }

    public class QuizSessionDto
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public Guid HostId { get; set; }
        public SessionStatus Status { get; set; }
        public int CurrentQuestion { get; set; }
        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? MezonChannelId { get; set; }
        public int? MaxParticipants { get; set; }
        public int ParticipantCount { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class JoinQuizSessionDto
    {
        public Guid UserId { get; set; }
    }

    public class ClearParticipantDto
    {
        public Guid UserId { get; set; }
    }

    public class SubmitAnswerDto
    {
        public Guid UserId { get; set; }
        public int SelectedOption { get; set; }
        public int? ResponseTimeMs { get; set; }
    }

    public class SessionParticipantDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int AnswersCount { get; set; }
        public int CorrectCount { get; set; }
        public int? Rank { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class SessionOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
