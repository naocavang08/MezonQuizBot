using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Domain.Entites;

namespace WebApp.Application.ManageQuizSession
{
    public interface IQuizSessionService
    {
        Task<List<QuizSessionDto>> GetAllSessions(Guid? QuizId);
        Task<QuizSession?> GetSession(Guid sessionId);
        Task<(SessionOperationResult Result, QuizSessionDto? Session)> CreateSession(CreateQuizSessionDto request, Guid hostId);
        Task<SessionOperationResult> JoinSession(Guid sessionId, JoinQuizSessionDto request);
        Task<SessionOperationResult> StartSession(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> PauseSession(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> ResumeSession(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> FinishSession(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> CancelSession(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> DeleteSession(Guid sessionId, Guid hostId);
        Task<SessionOperationResult> NextQuestion(Guid sessionId, Guid hostId);
        Task<(SessionOperationResult Result, QuizSessionQuestionDto? Question)> GetCurrentQuestion(Guid sessionId);
        Task<SessionOperationResult> SubmitAnswer(Guid sessionId, SubmitAnswerDto request);
        Task<List<SessionParticipantDto>> GetLeaderboard(Guid sessionId);
    }
}
