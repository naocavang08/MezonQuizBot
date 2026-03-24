using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WebApp.Data;
using WebApp.Domain.Entites;
using WebApp.Realtime;
using static WebApp.Domain.Enums.Status;
using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Application.ManageQuizSession.Dtos;

namespace WebApp.Application.ManageQuizSession.Services
{
    public class QuizSessionService : IQuizSessionService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDynamicLinkService _dynamicLinkService;
        private readonly IHubContext<QuizHub> _hubContext;

        public QuizSessionService(
            AppDbContext dbContext,
            IDynamicLinkService dynamicLinkService,
            IHubContext<QuizHub> hubContext)
        {
            _dbContext = dbContext;
            _dynamicLinkService = dynamicLinkService;
            _hubContext = hubContext;
        }

        public async Task<List<QuizSessionDto>> GetAllSessions(Guid? QuizId)
        {
            var sessionsQuery = _dbContext.QuizSessions
                .AsNoTracking()
                .Include(s => s.Quiz)
                .AsQueryable();

            if (QuizId.HasValue && QuizId.Value != Guid.Empty)
            {
                sessionsQuery = sessionsQuery.Where(s => s.QuizId == QuizId.Value);
            }

            var sessions = await sessionsQuery
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new QuizSessionDto
                {
                    Id = s.Id,
                    QuizId = s.QuizId,
                    QuizTitle = s.Quiz.Title,
                    HostId = s.HostId,
                    Status = s.Status,
                    CurrentQuestion = s.CurrentQuestion,
                    DeepLink = s.DeepLink,
                    QrCodeUrl = s.QrCodeUrl,
                    MezonChannelId = s.MezonChannelId,
                    MaxParticipants = s.MaxParticipants,
                    ParticipantCount = _dbContext.SessionParticipants.Count(p => p.SessionId == s.Id),
                    StartedAt = s.StartedAt,
                    FinishedAt = s.FinishedAt,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return sessions;
        }

        public async Task<QuizSessionDto?> GetSession(Guid sessionId)
        {
            var session = await _dbContext.QuizSessions
                .AsNoTracking()
                .Include(s => s.Quiz)
                .Where(s => s.Id == sessionId)
                .Select(s => new QuizSessionDto
                {
                    Id = s.Id,
                    QuizId = s.QuizId,
                    QuizTitle = s.Quiz.Title,
                    HostId = s.HostId,
                    Status = s.Status,
                    CurrentQuestion = s.CurrentQuestion,
                    DeepLink = s.DeepLink,
                    QrCodeUrl = s.QrCodeUrl,
                    MezonChannelId = s.MezonChannelId,
                    MaxParticipants = s.MaxParticipants,
                    ParticipantCount = _dbContext.SessionParticipants.Count(p => p.SessionId == s.Id),
                    StartedAt = s.StartedAt,
                    FinishedAt = s.FinishedAt,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (session is null)
            {
                return null;
            }
            return session;
        }

        public async Task<(SessionOperationResult Result, QuizSessionDto? Session)> CreateSession(CreateQuizSessionDto request, Guid hostId)
        {
            if (request is null || request.QuizId == Guid.Empty || hostId == Guid.Empty)
            {
                return (Fail("Invalid create session request."), null);
            }

            var quiz = await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.Id == request.QuizId);
            if (quiz is null)
            {
                return (Fail("Quiz not found."), null);
            }

            if (quiz.Status != QuizStatus.Published)
            {
                return (Fail("Only published quizzes can open sessions."), null);
            }

            if (hostId != quiz.CreatorId)
            {
                return (Fail("HostId must be the quiz creatorId."), null);
            }

            if (request.MaxParticipants.HasValue && request.MaxParticipants.Value < 1)
            {
                return (Fail("MaxParticipants must be greater than 0."), null);
            }

            var session = new QuizSession
            {
                Id = Guid.NewGuid(),
                QuizId = request.QuizId,
                HostId = hostId,
                Status = SessionStatus.Waiting,
                CurrentQuestion = 0,
                MezonChannelId = request.MezonChannelId,
                MaxParticipants = request.MaxParticipants,
                CreatedAt = DateTime.UtcNow
            };

            var links = _dynamicLinkService.BuildSessionLinks(session.Id, session.QuizId, session.HostId);
            session.DeepLink = links.DeepLink;
            session.QrCodeUrl = links.QrCodeUrl;

            _dbContext.QuizSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            var dto = new QuizSessionDto
            {
                Id = session.Id,
                QuizId = session.QuizId,
                QuizTitle = quiz.Title,
                HostId = session.HostId,
                Status = session.Status,
                CurrentQuestion = session.CurrentQuestion,
                DeepLink = session.DeepLink,
                QrCodeUrl = session.QrCodeUrl,
                MezonChannelId = session.MezonChannelId,
                MaxParticipants = session.MaxParticipants,
                ParticipantCount = 0,
                StartedAt = session.StartedAt,
                FinishedAt = session.FinishedAt,
                CreatedAt = session.CreatedAt
            };

            return (Success("Session created successfully."), dto);
        }

        public async Task<SessionOperationResult> JoinSession(Guid sessionId, JoinQuizSessionDto request)
        {
            if (request is null || request.UserId == Guid.Empty)
            {
                return Fail("Invalid join request.");
            }

            var session = await _dbContext.QuizSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session is null)
            {
                return Fail("Session not found.");
            }

            if (session.Status != SessionStatus.Waiting)
            {
                return Fail("Participants can join only while session is waiting.");
            }

            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                return Fail("User not found.");
            }

            var alreadyJoined = await _dbContext.SessionParticipants
                .AnyAsync(p => p.SessionId == sessionId && p.UserId == request.UserId);

            if (alreadyJoined)
            {
                return Success("Participant already joined.");
            }

            if (session.MaxParticipants.HasValue)
            {
                var currentCount = await _dbContext.SessionParticipants.CountAsync(p => p.SessionId == sessionId);
                if (currentCount >= session.MaxParticipants.Value)
                {
                    return Fail("Session is full.");
                }
            }

            _dbContext.SessionParticipants.Add(new SessionParticipant
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = request.UserId,
                TotalScore = 0,
                AnswersCount = 0,
                CorrectCount = 0,
                Rank = null,
                JoinedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Joined session successfully.");
        }

        public async Task<SessionOperationResult> ClearParticipant(Guid sessionId, Guid hostId, ClearParticipantDto request)
        {
            if (request is null || request.UserId == Guid.Empty)
            {
                return Fail("Invalid clear participant request.");
            }

            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status == SessionStatus.Finished || session.Status == SessionStatus.Cancelled)
            {
                return Fail("Can not clear participant from closed session.");
            }

            var participant = await _dbContext.SessionParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == request.UserId);

            if (participant is null)
            {
                return Fail("Participant is not in this session.");
            }

            _dbContext.SessionParticipants.Remove(participant);
            await _dbContext.SaveChangesAsync();

            await BroadcastSessionStateChanged(session);
            return Success("Participant cleared successfully.");
        }

        public async Task<SessionOperationResult> StartSession(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status != SessionStatus.Waiting)
            {
                return Fail("Only waiting session can be started.");
            }

            session.Status = SessionStatus.Active;
            session.StartedAt = DateTime.UtcNow;
            session.FinishedAt = null;
            session.CurrentQuestion = 0;

            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Session started successfully.");
        }

        public async Task<SessionOperationResult> PauseSession(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status != SessionStatus.Active)
            {
                return Fail("Only active session can be paused.");
            }

            session.Status = SessionStatus.Paused;
            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Session paused successfully.");
        }

        public async Task<SessionOperationResult> ResumeSession(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status != SessionStatus.Paused)
            {
                return Fail("Only paused session can be resumed.");
            }

            session.Status = SessionStatus.Active;
            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Session resumed successfully.");
        }

        public async Task<SessionOperationResult> FinishSession(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status != SessionStatus.Active && session.Status != SessionStatus.Paused)
            {
                return Fail("Only active or paused session can be finished.");
            }

            session.Status = SessionStatus.Finished;
            session.FinishedAt = DateTime.UtcNow;

            await RecalculateRanks(sessionId);
            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Session finished successfully.");
        }

        public async Task<SessionOperationResult> CancelSession(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status == SessionStatus.Finished || session.Status == SessionStatus.Cancelled)
            {
                return Fail("Session is already closed.");
            }

            session.Status = SessionStatus.Cancelled;
            session.FinishedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Session cancelled successfully.");
        }

        public async Task<SessionOperationResult> DeleteSession(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            _dbContext.QuizSessions.Remove(session);
            await _dbContext.SaveChangesAsync();
            return Success("Session deleted successfully.");
        }

        public async Task<SessionOperationResult> NextQuestion(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostAction(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            if (session.Status != SessionStatus.Active)
            {
                return Fail("Session must be active to move next question.");
            }

            var quiz = await _dbContext.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == session.QuizId);
            if (quiz is null)
            {
                return Fail("Quiz not found for this session.");
            }

            var totalQuestions = quiz.Questions?.Count ?? 0;
            if (totalQuestions == 0)
            {
                return Fail("Quiz has no question.");
            }

            if (session.CurrentQuestion >= totalQuestions - 1)
            {
                return Fail("Already at last question.");
            }

            session.CurrentQuestion += 1;
            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Moved to next question.");
        }

        public async Task<(SessionOperationResult Result, QuizSessionQuestionDto? Question)> GetCurrentQuestion(Guid sessionId)
        {
            var session = await _dbContext.QuizSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session is null)
            {
                return (Fail("Session not found."), null);
            }

            if (session.Status != SessionStatus.Active && session.Status != SessionStatus.Paused)
            {
                return (Fail("Session is not active."), null);
            }

            var quiz = await _dbContext.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == session.QuizId);

            if (quiz is null)
            {
                return (Fail("Quiz not found for this session."), null);
            }

            var question = ResolveQuestion(quiz, session.CurrentQuestion);
            if (question is null)
            {
                return (Fail("Current question is invalid."), null);
            }

            var dto = new QuizSessionQuestionDto
            {
                SessionId = session.Id,
                QuestionIndex = session.CurrentQuestion,
                Content = question.Content,
                MediaUrl = question.MediaUrl,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                Options = question.Options
                    .Select(option => new QuizSessionQuestionOptionDto
                    {
                        Index = option.Index,
                        Content = option.Content
                    })
                    .ToList()
            };

            return (Success("Current question loaded successfully."), dto);
        }

        public async Task<SessionOperationResult> SubmitAnswer(Guid sessionId, SubmitAnswerDto request)
        {
            if (request is null || request.UserId == Guid.Empty)
            {
                return Fail("Invalid answer request.");
            }

            var session = await _dbContext.QuizSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session is null)
            {
                return Fail("Session not found.");
            }

            if (session.Status != SessionStatus.Active)
            {
                return Fail("Can only submit answer while session is active.");
            }

            var participant = await _dbContext.SessionParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == request.UserId);

            if (participant is null)
            {
                return Fail("User is not in this session.");
            }

            var quiz = await _dbContext.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == session.QuizId);
            if (quiz is null)
            {
                return Fail("Quiz not found for this session.");
            }

            var question = ResolveQuestion(quiz, session.CurrentQuestion);
            if (question is null)
            {
                return Fail("Current question is invalid.");
            }

            var duplicatedAnswer = await _dbContext.Answers.AnyAsync(a =>
                a.SessionId == sessionId &&
                a.UserId == request.UserId &&
                a.QuestionIndex == session.CurrentQuestion);

            if (duplicatedAnswer)
            {
                return Fail("Answer already submitted for current question.");
            }

            var selectedOption = ResolveSelectedOption(question, request.SelectedOption);
            if (selectedOption is null)
            {
                return Fail("Selected option is invalid.");
            }

            var points = selectedOption.IsCorrect ? question.Points : 0;

            _dbContext.Answers.Add(new Answer
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = request.UserId,
                QuestionIndex = session.CurrentQuestion,
                SelectedOption = request.SelectedOption,
                IsCorrect = selectedOption.IsCorrect,
                PointsEarned = points,
                ResponseTimeMs = request.ResponseTimeMs,
                AnsweredAt = DateTime.UtcNow
            });

            participant.AnswersCount += 1;
            if (selectedOption.IsCorrect)
            {
                participant.CorrectCount += 1;
            }

            participant.TotalScore += points;

            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return Success("Answer submitted successfully.");
        }

        public async Task<List<SessionParticipantDto>> GetLeaderboard(Guid sessionId)
        {
            var participants = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == sessionId)
                .OrderByDescending(p => p.TotalScore)
                .ThenByDescending(p => p.CorrectCount)
                .ThenBy(p => p.JoinedAt)
                .Select(p => new SessionParticipantDto
                {
                    UserId = p.UserId,
                    DisplayName = string.IsNullOrEmpty(p.User.DisplayName) ? p.User.Username : p.User.DisplayName,
                    TotalScore = p.TotalScore,
                    AnswersCount = p.AnswersCount,
                    CorrectCount = p.CorrectCount,
                    Rank = p.Rank,
                    JoinedAt = p.JoinedAt
                })
                .ToListAsync();

            return participants;
        }

        private async Task<QuizSession?> GetSessionForHostAction(Guid sessionId, Guid hostId)
        {
            if (hostId == Guid.Empty)
            {
                return null;
            }

            var session = await _dbContext.QuizSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session is null)
            {
                return null;
            }

            if (session.HostId != hostId)
            {
                return null;
            }

            return session;
        }

        private async Task RecalculateRanks(Guid sessionId)
        {
            var participants = await _dbContext.SessionParticipants
                .Where(p => p.SessionId == sessionId)
                .OrderByDescending(p => p.TotalScore)
                .ThenByDescending(p => p.CorrectCount)
                .ThenBy(p => p.JoinedAt)
                .ToListAsync();

            for (var index = 0; index < participants.Count; index++)
            {
                participants[index].Rank = index + 1;
            }
        }

        private static QuizQuestion? ResolveQuestion(Quiz quiz, int currentQuestion)
        {
            if (quiz.Questions is null || quiz.Questions.Count == 0)
            {
                return null;
            }

            if (currentQuestion >= 0 && currentQuestion < quiz.Questions.Count)
            {
                return quiz.Questions[currentQuestion];
            }

            var byQuestionIndexField = quiz.Questions.FirstOrDefault(q => q.Index == currentQuestion);
            if (byQuestionIndexField is not null)
            {
                return byQuestionIndexField;
            }

            return null;
        }

        private static QuizOption? ResolveSelectedOption(QuizQuestion question, int selectedOption)
        {
            var byOptionIndexField = question.Options.FirstOrDefault(o => o.Index == selectedOption);
            if (byOptionIndexField is not null)
            {
                return byOptionIndexField;
            }

            if (selectedOption >= 0 && selectedOption < question.Options.Count)
            {
                return question.Options[selectedOption];
            }

            return null;
        }

        private static SessionOperationResult Success(string message)
        {
            return new SessionOperationResult
            {
                Success = true,
                Message = message
            };
        }

        private static SessionOperationResult Fail(string message)
        {
            return new SessionOperationResult
            {
                Success = false,
                Message = message
            };
        }

        private async Task BroadcastSessionStateChanged(QuizSession session)
        {
            var payload = new SessionStateChangedDto
            {
                SessionId = session.Id,
                Status = session.Status,
                CurrentQuestion = session.CurrentQuestion
            };

            await _hubContext.Clients
                .Group(session.Id.ToString())
                .SendAsync("SessionStateChanged", payload);
        }
    }
}
