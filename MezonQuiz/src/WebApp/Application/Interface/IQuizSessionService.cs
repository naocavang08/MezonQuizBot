using WebApp.Application.Dtos;

namespace WebApp.Application.Interface
{
    public interface IQuizSessionService
    {
        Task<PagedQuizListDto<QuizSessionDto>> GetSessionsAsync(QuizSessionQuery query);
        Task<QuizSessionDto?> GetSessionDetailsAsync(Guid sessionId);
        Task<(SessionOperationResult Result, QuizSessionDto? Session)> CreateSessionAsync(CreateQuizSessionDto request, Guid hostId);
        Task<SessionOperationResult> JoinSessionAsync(Guid sessionId, JoinQuizSessionDto request);
        Task<SessionOperationResult> StartSessionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> PauseSessionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> ResumeSessionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> FinishSessionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> CancelSessionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> DeleteSessionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> NextQuestionAsync(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> SubmitAnswerAsync(Guid sessionId, SubmitAnswerDto request);
        Task<List<SessionParticipantDto>> GetLeaderboardAsync(Guid sessionId);
    }
}
