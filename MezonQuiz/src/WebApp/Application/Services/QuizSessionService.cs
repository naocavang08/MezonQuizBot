using Microsoft.EntityFrameworkCore;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Data;
using WebApp.Domain.Entites;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.Services
{
    public class QuizSessionService : IQuizSessionService
    {
        private readonly AppDbContext _dbContext;

        public QuizSessionService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedQuizListDto<QuizSessionDto>> GetSessionsAsync(QuizSessionQuery query)
        {
            query ??= new QuizSessionQuery();

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);

            var sessionsQuery = _dbContext.QuizSessions
                .AsNoTracking()
                .Include(s => s.Quiz)
                .Include(s => s.Host)
                .AsQueryable();

            if (query.HostId.HasValue && query.HostId.Value != Guid.Empty)
            {
                sessionsQuery = sessionsQuery.Where(s => s.HostId == query.HostId.Value);
            }

            if (query.QuizId.HasValue && query.QuizId.Value != Guid.Empty)
            {
                sessionsQuery = sessionsQuery.Where(s => s.QuizId == query.QuizId.Value);
            }

            if (query.Status.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.Status == query.Status.Value);
            }

            var totalCount = await sessionsQuery.CountAsync();
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var sessions = await sessionsQuery
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new PagedQuizListDto<QuizSessionDto>
            {
                Items = sessions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<QuizSessionDto?> GetSessionDetailsAsync(Guid sessionId)
        {
            var session = await _dbContext.QuizSessions
                .AsNoTracking()
                .Include(s => s.Quiz)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session is null)
            {
                return null;
            }

            var participantCount = await _dbContext.SessionParticipants.CountAsync(p => p.SessionId == session.Id);

            return new QuizSessionDto
            {
                Id = session.Id,
                QuizId = session.QuizId,
                QuizTitle = session.Quiz.Title,
                HostId = session.HostId,
                Status = session.Status,
                CurrentQuestion = session.CurrentQuestion,
                DeepLink = session.DeepLink,
                QrCodeUrl = session.QrCodeUrl,
                MezonChannelId = session.MezonChannelId,
                MaxParticipants = session.MaxParticipants,
                ParticipantCount = participantCount,
                StartedAt = session.StartedAt,
                FinishedAt = session.FinishedAt,
                CreatedAt = session.CreatedAt
            };
        }

        public async Task<(SessionOperationResult Result, QuizSessionDto? Session)> CreateSessionAsync(CreateQuizSessionDto request, Guid hostId)
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
                DeepLink = request.DeepLink,
                QrCodeUrl = request.QrCodeUrl,
                MezonChannelId = request.MezonChannelId,
                MaxParticipants = request.MaxParticipants,
                CreatedAt = DateTime.UtcNow
            };

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

        public async Task<SessionOperationResult> JoinSessionAsync(Guid sessionId, JoinQuizSessionDto request)
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
            return Success("Joined session successfully.");
        }

        public async Task<SessionOperationResult> StartSessionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
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
            return Success("Session started successfully.");
        }

        public async Task<SessionOperationResult> PauseSessionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
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
            return Success("Session paused successfully.");
        }

        public async Task<SessionOperationResult> ResumeSessionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
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
            return Success("Session resumed successfully.");
        }

        public async Task<SessionOperationResult> FinishSessionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
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

            await RecalculateRanksAsync(sessionId);
            await _dbContext.SaveChangesAsync();
            return Success("Session finished successfully.");
        }

        public async Task<SessionOperationResult> CancelSessionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
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
            return Success("Session cancelled successfully.");
        }

        public async Task<SessionOperationResult> DeleteSessionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
            if (session is null)
            {
                return Fail("Session not found or host is not allowed.");
            }

            _dbContext.QuizSessions.Remove(session);
            await _dbContext.SaveChangesAsync();
            return Success("Session deleted successfully.");
        }

        public async Task<SessionOperationResult> NextQuestionAsync(Guid sessionId, Guid hostId)
        {
            var session = await GetSessionForHostActionAsync(sessionId, hostId);
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
            return Success("Moved to next question.");
        }

        public async Task<SessionOperationResult> SubmitAnswerAsync(Guid sessionId, SubmitAnswerDto request)
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
            return Success("Answer submitted successfully.");
        }

        public async Task<List<SessionParticipantDto>> GetLeaderboardAsync(Guid sessionId)
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
                    TotalScore = p.TotalScore,
                    AnswersCount = p.AnswersCount,
                    CorrectCount = p.CorrectCount,
                    Rank = p.Rank,
                    JoinedAt = p.JoinedAt
                })
                .ToListAsync();

            return participants;
        }

        private async Task<QuizSession?> GetSessionForHostActionAsync(Guid sessionId, Guid hostId)
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

        private async Task RecalculateRanksAsync(Guid sessionId)
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

            var byQuestionIndexField = quiz.Questions.FirstOrDefault(q => q.Index == currentQuestion);
            if (byQuestionIndexField is not null)
            {
                return byQuestionIndexField;
            }

            if (currentQuestion >= 0 && currentQuestion < quiz.Questions.Count)
            {
                return quiz.Questions[currentQuestion];
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
    }
}
