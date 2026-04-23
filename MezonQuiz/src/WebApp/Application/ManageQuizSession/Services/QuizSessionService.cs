using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Mezon_sdk.Models;
using Mezon_sdk.Structures;
using WebApp.Data;
using WebApp.Domain.Entites;
using WebApp.Realtime;
using static WebApp.Domain.Enums.Status;
using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Formatters;
using WebApp.Integration.Mezon;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace WebApp.Application.ManageQuizSession.Services
{
    public class QuizSessionService : IQuizSessionService
    {
        private static readonly ConcurrentDictionary<string, DateTime> RecentQuestionDispatches = new();
        private static readonly TimeSpan QuestionDispatchDedupWindow = TimeSpan.FromSeconds(3);

        private readonly AppDbContext _dbContext;
        private readonly IDynamicLinkService _dynamicLinkService;
        private readonly IHubContext<QuizHub> _hubContext;
        private readonly MezonBotHostedService _mezonBotHostedService;
        private readonly ILogger<QuizSessionService> _logger;

        public QuizSessionService(
            AppDbContext dbContext,
            IDynamicLinkService dynamicLinkService,
            IHubContext<QuizHub> hubContext,
            MezonBotHostedService mezonBotHostedService,
            ILogger<QuizSessionService> logger)
        {
            _dbContext = dbContext;
            _dynamicLinkService = dynamicLinkService;
            _hubContext = hubContext;
            _mezonBotHostedService = mezonBotHostedService;
            _logger = logger;
        }

        public async Task<List<QuizSessionDto>> GetAllSessions(Guid? QuizId)
        {
            var sessionsQuery = _dbContext.QuizSessions
                .AsNoTracking()
                .AsQueryable();

            var participantCountsQuery = _dbContext.SessionParticipants
                .AsNoTracking()
                .GroupBy(p => p.SessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    Count = g.Count()
                });

            if (QuizId.HasValue && QuizId.Value != Guid.Empty)
            {
                sessionsQuery = sessionsQuery.Where(s => s.QuizId == QuizId.Value);
            }

            var sessions = await sessionsQuery
                .GroupJoin(
                    participantCountsQuery,
                    session => session.Id,
                    count => count.SessionId,
                    (session, counts) => new { Session = session, ParticipantCount = counts.Select(c => c.Count).FirstOrDefault() })
                .OrderByDescending(x => x.Session.CreatedAt)
                .Select(x => new QuizSessionDto
                {
                    Id = x.Session.Id,
                    Code = x.Session.Code ?? string.Empty,
                    QuizId = x.Session.QuizId,
                    QuizTitle = x.Session.Quiz.Title,
                    HostId = x.Session.HostId,
                    Status = x.Session.Status,
                    CurrentQuestion = x.Session.CurrentQuestion,
                    DeepLink = x.Session.DeepLink,
                    QrCodeUrl = x.Session.QrCodeUrl,
                    MezonChannelId = x.Session.MezonChannelId,
                    MaxParticipants = x.Session.MaxParticipants,
                    ParticipantCount = x.ParticipantCount,
                    StartedAt = x.Session.StartedAt,
                    FinishedAt = x.Session.FinishedAt,
                    CreatedAt = x.Session.CreatedAt
                })
                .ToListAsync();

            return sessions;
        }

        public async Task<QuizSessionDto?> GetSession(Guid sessionId)
        {
            var participantCountsQuery = _dbContext.SessionParticipants
                .AsNoTracking()
                .GroupBy(p => p.SessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    Count = g.Count()
                });

            var session = await _dbContext.QuizSessions
                .AsNoTracking()
                .Where(s => s.Id == sessionId)
                .GroupJoin(
                    participantCountsQuery,
                    s => s.Id,
                    count => count.SessionId,
                    (s, counts) => new { Session = s, ParticipantCount = counts.Select(c => c.Count).FirstOrDefault() })
                .Select(x => new QuizSessionDto
                {
                    Id = x.Session.Id,
                    Code = x.Session.Code ?? string.Empty,
                    QuizId = x.Session.QuizId,
                    QuizTitle = x.Session.Quiz.Title,
                    HostId = x.Session.HostId,
                    Status = x.Session.Status,
                    CurrentQuestion = x.Session.CurrentQuestion,
                    DeepLink = x.Session.DeepLink,
                    QrCodeUrl = x.Session.QrCodeUrl,
                    MezonChannelId = x.Session.MezonChannelId,
                    MaxParticipants = x.Session.MaxParticipants,
                    ParticipantCount = x.ParticipantCount,
                    StartedAt = x.Session.StartedAt,
                    FinishedAt = x.Session.FinishedAt,
                    CreatedAt = x.Session.CreatedAt
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

            var existSession = await _dbContext.QuizSessions
                .FirstOrDefaultAsync(s => s.QuizId == request.QuizId && s.Status != SessionStatus.Finished && s.Status != SessionStatus.Cancelled);
            if (existSession != null)
            {
                return (Fail("Quiz already has an active session."), null);
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

            var links = _dynamicLinkService.BuildSessionLinks(session.Id);
            session.DeepLink = links.DeepLink;
            session.QrCodeUrl = links.QrCodeUrl;
            session.Code = links.Code;

            _dbContext.QuizSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            var dto = new QuizSessionDto
            {
                Id = session.Id,
                Code = session.Code,
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

        public async Task<SessionOperationResult> JoinByCode(string code, JoinQuizSessionDto request)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Fail("Session code is required.");
            }

            if (request is null || request.UserId == Guid.Empty)
            {
                return Fail("Invalid join request.");
            }

            var session = await _dbContext.QuizSessions
                .FirstOrDefaultAsync(s => s.Code == code.ToUpperInvariant());

            if (session is null)
            {
                return Fail("Session not found for the given code.");
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
                .AnyAsync(p => p.SessionId == session.Id && p.UserId == request.UserId);

            if (alreadyJoined)
            {
                return new SessionOperationResult { Success = true, Message = "Participant already joined.", SessionId = session.Id };
            }

            var joinedAnotherOpenSession = await _dbContext.SessionParticipants
                .Where(p => p.UserId == request.UserId && p.SessionId != session.Id)
                .Join(
                    _dbContext.QuizSessions,
                    participant => participant.SessionId,
                    quizSession => quizSession.Id,
                    (participant, quizSession) => quizSession.Status)
                .AnyAsync(status =>
                    status != SessionStatus.Finished && status != SessionStatus.Cancelled);

            if (joinedAnotherOpenSession)
            {
                return Fail("User has already joined another session.");
            }

            if (session.MaxParticipants.HasValue)
            {
                var currentCount = await _dbContext.SessionParticipants.CountAsync(p => p.SessionId == session.Id);
                if (currentCount >= session.MaxParticipants.Value)
                {
                    return Fail("Session is full.");
                }
            }

            _dbContext.SessionParticipants.Add(new SessionParticipant
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserId = request.UserId,
                TotalScore = 0,
                AnswersCount = 0,
                CorrectCount = 0,
                Rank = null,
                CurrentQuestionIndex = 0,
                CompletedAt = null,
                JoinedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return new SessionOperationResult { Success = true, Message = "Joined session successfully.", SessionId = session.Id };
        }

        public async Task<SessionOperationResult> LeaveSessions(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Fail("Invalid exit request.");
            }

            var openSessions = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Join(
                    _dbContext.QuizSessions.AsNoTracking().Where(s => s.Status != SessionStatus.Finished && s.Status != SessionStatus.Cancelled),
                    participant => participant.SessionId,
                    session => session.Id,
                    (participant, session) => session)
                .Distinct()
                .ToListAsync();

            if (openSessions.Count == 0)
            {
                return Fail("User is not in any open session.");
            }

            var openSessionIds = openSessions.Select(s => s.Id).ToList();

            var participants = await _dbContext.SessionParticipants
                .Where(p => p.UserId == userId && openSessionIds.Contains(p.SessionId))
                .ToListAsync();

            _dbContext.SessionParticipants.RemoveRange(participants);
            await _dbContext.SaveChangesAsync();

            foreach (var session in openSessions)
            {
                await BroadcastSessionStateChanged(session);
            }

            return Success($"Left session successfully.");
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

            var participants = await _dbContext.SessionParticipants
                .Where(p => p.SessionId == sessionId)
                .ToListAsync();

            foreach (var participant in participants)
            {
                participant.CurrentQuestionIndex = 0;
                participant.CompletedAt = null;
            }

            await _dbContext.SaveChangesAsync();
            await SendCurrentQuestionToParticipants(session);
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
            await NotifySessionParticipantsAsync(
                session,
                title: "Session Paused",
                description: "Host has paused the quiz session. Please wait for resume.",
                color: "#F59E0B");
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
            await NotifySessionParticipantsAsync(
                session,
                title: "Session Resumed",
                description: "Quiz session has resumed. Continue answering your current question.",
                color: "#22C55E");
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
            await NotifySessionParticipantsAsync(
                session,
                title: "Session Finished",
                description: "Quiz session has finished. Thank you for participating.",
                color: "#64748B");
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
            _ = sessionId;
            _ = hostId;
            return Fail("NextQuestion is deprecated. Question progress is now tracked per participant.");
        }

        public async Task<(SessionOperationResult Result, QuizSessionQuestionDto? Question)> GetCurrentQuestion(Guid sessionId, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return (Fail("Invalid user."), null);
            }

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

            var participant = await _dbContext.SessionParticipants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId);

            if (participant is null)
            {
                return (Fail("User is not in this session."), null);
            }

            var quiz = await _dbContext.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == session.QuizId);

            if (quiz is null)
            {
                return (Fail("Quiz not found for this session."), null);
            }

            var settings = GetEffectiveQuizSettings(quiz);
            var orderedQuestions = GetOrderedQuestionsForParticipant(quiz, session.Id, userId, settings);

            var questionIndex = participant.CurrentQuestionIndex;
            var totalQuestions = orderedQuestions.Count;
            if (questionIndex >= totalQuestions)
            {
                return (Fail("Participant has completed all questions."), null);
            }

            var question = ResolveQuestionByProgress(orderedQuestions, questionIndex);
            if (question is null)
            {
                return (Fail("Current question is invalid."), null);
            }

            var presentedOptions = GetPresentedOptionsForParticipant(
                question,
                session.Id,
                userId,
                questionIndex,
                settings);

            var dto = new QuizSessionQuestionDto
            {
                SessionId = session.Id,
                QuestionIndex = questionIndex,
                Content = question.Content,
                MediaUrl = question.MediaUrl,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                QuestionType = question.QuestionType,
                Options = presentedOptions
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

            if (participant.CompletedAt.HasValue)
            {
                return Fail("Participant has completed all questions.");
            }

            var quiz = await _dbContext.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == session.QuizId);
            if (quiz is null)
            {
                return Fail("Quiz not found for this session.");
            }

            var settings = GetEffectiveQuizSettings(quiz);
            var orderedQuestions = GetOrderedQuestionsForParticipant(quiz, session.Id, request.UserId, settings);

            var questionIndex = participant.CurrentQuestionIndex;
            var totalQuestions = orderedQuestions.Count;
            if (questionIndex >= totalQuestions)
            {
                participant.CompletedAt ??= DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return Fail("Participant has completed all questions.");
            }

            var question = ResolveQuestionByProgress(orderedQuestions, questionIndex);
            if (question is null)
            {
                return Fail("Current question is invalid.");
            }

            var duplicatedAnswer = await _dbContext.Answers.AnyAsync(a =>
                a.SessionId == sessionId &&
                a.UserId == request.UserId &&
                a.QuestionIndex == questionIndex);

            if (duplicatedAnswer)
            {
                return Fail("Answer already submitted for current question.");
            }

            var selectedOptions = ResolveSelectedOptions(question, request);
            if (selectedOptions.Count == 0)
            {
                return Fail("Selected option is invalid.");
            }

            var presentedOptions = GetPresentedOptionsForParticipant(
                question,
                session.Id,
                request.UserId,
                questionIndex,
                settings);

            var displayIndexByOptionIndex = presentedOptions
                .Select((option, position) => new { option.Index, DisplayIndex = position + 1 })
                .GroupBy(item => item.Index)
                .ToDictionary(group => group.Key, group => group.First().DisplayIndex);

            var selectedOptionDisplays = selectedOptions
                .Select(optionIndex => displayIndexByOptionIndex.TryGetValue(optionIndex, out var displayIndex)
                    ? displayIndex
                    : optionIndex)
                .Distinct()
                .OrderBy(index => index)
                .ToList();
            var selectedOptionDisplay = selectedOptionDisplays[0];
            var correctOptionDisplays = question.Options
                .Where(option => option.IsCorrect)
                .OrderBy(option => option.Index)
                .Select(option => displayIndexByOptionIndex.TryGetValue(option.Index, out var displayIndex)
                    ? displayIndex
                    : option.Index)
                .Distinct()
                .OrderBy(index => index)
                .ToList();

            var isCorrect = IsCorrectAnswer(question, selectedOptions);
            var points = isCorrect ? question.Points : 0;

            _dbContext.Answers.Add(new Answer
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = request.UserId,
                QuestionIndex = questionIndex,
                SelectedOption = selectedOptions[0],
                IsCorrect = isCorrect,
                PointsEarned = points,
                ResponseTimeMs = request.ResponseTimeMs,
                AnsweredAt = DateTime.UtcNow
            });

            participant.AnswersCount += 1;
            if (isCorrect)
            {
                participant.CorrectCount += 1;
            }

            participant.TotalScore += points;
            participant.CurrentQuestionIndex += 1;

            if (participant.CurrentQuestionIndex >= totalQuestions)
            {
                participant.CompletedAt ??= DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            if (!request.SkipAutoDispatchNextQuestion && participant.CompletedAt is null)
            {
                await SendCurrentQuestionToParticipant(session, participant.UserId);
            }

            await BroadcastSessionStateChanged(session);
            return new SessionOperationResult
            {
                Success = true,
                Message = "Answer submitted successfully.",
                SessionId = sessionId,
                IsCorrect = isCorrect,
                PointsEarned = points,
                TotalScore = participant.TotalScore,
                AnswersCount = participant.AnswersCount,
                CorrectCount = participant.CorrectCount,
                QuestionIndex = questionIndex,
                SelectedOptionDisplays = selectedOptionDisplays,
                SelectedOptionDisplay = selectedOptionDisplay,
                CanRevealCorrectAnswer = settings.ShowCorrectAnswer,
                CorrectOptionDisplays = settings.ShowCorrectAnswer ? correctOptionDisplays : new List<int>()
            };
        }

        public async Task<List<SessionParticipantDto>> GetLeaderboard(Guid sessionId)
        {
            var participantRows = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == sessionId)
                .OrderByDescending(p => p.TotalScore)
                .ThenByDescending(p => p.CorrectCount)
                .ThenBy(p => p.CompletedAt.HasValue ? 0 : 1)
                .ThenBy(p => p.CompletedAt)
                .ThenBy(p => p.JoinedAt)
                .Select(p => new
                {
                    p.UserId,
                    DisplayName = string.IsNullOrEmpty(p.User.DisplayName) ? p.User.Username : p.User.DisplayName,
                    p.User.AvatarUrl,
                    p.TotalScore,
                    p.AnswersCount,
                    p.CorrectCount,
                    p.Rank,
                    p.CurrentQuestionIndex,
                    p.CompletedAt,
                    SessionStartedAt = p.Session.StartedAt,
                    p.JoinedAt
                })
                .ToListAsync();

            var participants = participantRows
                .Select(p => new SessionParticipantDto
                {
                    UserId = p.UserId,
                    DisplayName = p.DisplayName,
                    AvatarUrl = p.AvatarUrl,
                    TotalScore = p.TotalScore,
                    AnswersCount = p.AnswersCount,
                    CorrectCount = p.CorrectCount,
                    Rank = p.Rank,
                    CurrentQuestionIndex = p.CurrentQuestionIndex,
                    CompletedAt = p.CompletedAt,
                    CompletionDurationSeconds = p.CompletedAt.HasValue && p.SessionStartedAt.HasValue
                        ? (int?)Math.Max(0, (p.CompletedAt.Value - p.SessionStartedAt.Value).TotalSeconds)
                        : null,
                    JoinedAt = p.JoinedAt
                })
                .ToList();

            return participants;
        }

        public async Task<List<SessionParticipantDto>> GetQuizLeaderboard(Guid quizId)
        {
            var participantRows = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.Session.QuizId == quizId && p.Session.Status == SessionStatus.Finished)
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    DisplayName = g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.User.DisplayName ?? g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.User.Username,
                    AvatarUrl = g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.User.AvatarUrl,
                    TotalScore = g.Max(p => p.TotalScore),
                    AnswersCount = g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.AnswersCount,
                    CorrectCount = g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.CorrectCount,
                    CompletedAt = g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.CompletedAt,
                    SessionStartedAt = g.OrderByDescending(p => p.TotalScore).FirstOrDefault()!.Session.StartedAt,
                    JoinedAt = g.Min(p => p.JoinedAt),
                    TotalSessionsCount = g.Count()
                })
                .ToListAsync();

            var participants = participantRows
                .Select(p => new SessionParticipantDto
                {
                    UserId = p.UserId,
                    DisplayName = p.DisplayName,
                    AvatarUrl = p.AvatarUrl,
                    TotalScore = p.TotalScore,
                    AnswersCount = p.AnswersCount,
                    CorrectCount = p.CorrectCount,
                    CompletedAt = p.CompletedAt,
                    CompletionDurationSeconds = p.CompletedAt.HasValue && p.SessionStartedAt.HasValue
                        ? (int?)Math.Max(0, (p.CompletedAt.Value - p.SessionStartedAt.Value).TotalSeconds)
                        : null,
                    JoinedAt = p.JoinedAt,
                    TotalSessionsCount = p.TotalSessionsCount
                })
                .OrderByDescending(p => p.TotalScore)
                .ThenBy(p => p.TotalSessionsCount)
                .ThenBy(p => p.CompletionDurationSeconds.HasValue ? 0 : 1)
                .ThenBy(p => p.CompletionDurationSeconds)
                .ThenByDescending(p => p.CorrectCount)
                .ThenBy(p => p.JoinedAt)
                .ToList();

            for (int i = 0; i < participants.Count; i++)
            {
                participants[i].Rank = i + 1;
            }

            return participants;
        }

        public async Task DispatchCurrentQuestionToParticipant(Guid sessionId, Guid userId)
        {
            var session = await _dbContext.QuizSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session is null)
            {
                return;
            }

            if (session.Status != SessionStatus.Active)
            {
                return;
            }

            await SendCurrentQuestionToParticipant(session, userId);
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
                .ThenBy(p => p.CompletedAt.HasValue ? 0 : 1)
                .ThenBy(p => p.CompletedAt)
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

        private static List<int> ResolveSelectedOptions(QuizQuestion question, SubmitAnswerDto request)
        {
            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                var requested = request.SelectedOptions ?? new List<int>();
                var normalized = new List<int>();

                foreach (var candidate in requested.Distinct())
                {
                    var option = ResolveSelectedOption(question, candidate);
                    if (option is null)
                    {
                        return new List<int>();
                    }

                    normalized.Add(option.Index);
                }

                return normalized;
            }

            var single = ResolveSelectedOption(question, request.SelectedOption);
            if (single is null)
            {
                return new List<int>();
            }

            return new List<int> { single.Index };
        }

        private static bool IsCorrectAnswer(QuizQuestion question, List<int> selectedOptions)
        {
            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                var correctOptionIndexes = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Index)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();

                var submittedIndexes = selectedOptions
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();

                return correctOptionIndexes.SequenceEqual(submittedIndexes);
            }

            var selectedIndex = selectedOptions[0];
            return question.Options.Any(o => o.Index == selectedIndex && o.IsCorrect);
        }

        private static QuizSettings GetEffectiveQuizSettings(Quiz quiz)
        {
            var settings = quiz.Settings ?? new QuizSettings();
            return new QuizSettings
            {
                ShuffleQuestions = settings.ShuffleQuestions,
                ShuffleOptions = settings.ShuffleOptions,
                ShowCorrectAnswer = settings.ShowCorrectAnswer
            };
        }

        private static List<QuizQuestion> GetOrderedQuestionsForParticipant(
            Quiz quiz,
            Guid sessionId,
            Guid userId,
            QuizSettings settings)
        {
            var questions = (quiz.Questions ?? new List<QuizQuestion>()).ToList();
            if (!settings.ShuffleQuestions || questions.Count <= 1)
            {
                return questions;
            }

            var seedPrefix = $"session:{sessionId:N}:user:{userId:N}:questions";
            return questions
                .Select((question, position) => new
                {
                    Question = question,
                    Position = position,
                    Key = ComputeDeterministicOrderKey(seedPrefix, BuildQuestionStableKey(question, position))
                })
                .OrderBy(item => item.Key)
                .ThenBy(item => item.Position)
                .Select(item => item.Question)
                .ToList();
        }

        private static List<QuizOption> GetPresentedOptionsForParticipant(
            QuizQuestion question,
            Guid sessionId,
            Guid userId,
            int questionProgressIndex,
            QuizSettings settings)
        {
            var options = (question.Options ?? new List<QuizOption>())
                .Select(option => new QuizOption
                {
                    Id = option.Id,
                    Index = option.Index,
                    Content = option.Content,
                    IsCorrect = option.IsCorrect
                })
                .ToList();

            if (!settings.ShuffleOptions || options.Count <= 1)
            {
                return options;
            }

            var seedPrefix = $"session:{sessionId:N}:user:{userId:N}:q:{questionProgressIndex}:options";
            return options
                .Select((option, position) => new
                {
                    Option = option,
                    Position = position,
                    Key = ComputeDeterministicOrderKey(seedPrefix, BuildOptionStableKey(option, position))
                })
                .OrderBy(item => item.Key)
                .ThenBy(item => item.Position)
                .Select(item => item.Option)
                .ToList();
        }

        private static QuizQuestion? ResolveQuestionByProgress(List<QuizQuestion> orderedQuestions, int progressIndex)
        {
            if (progressIndex < 0 || progressIndex >= orderedQuestions.Count)
            {
                return null;
            }

            return orderedQuestions[progressIndex];
        }

        private static ulong ComputeDeterministicOrderKey(string seedPrefix, string itemKey)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedPrefix}:{itemKey}"));
            return BitConverter.ToUInt64(bytes, 0);
        }

        private static string BuildQuestionStableKey(QuizQuestion question, int fallbackPosition)
        {
            return $"qid:{question.Id};qidx:{question.Index};content:{question.Content};pos:{fallbackPosition}";
        }

        private static string BuildOptionStableKey(QuizOption option, int fallbackPosition)
        {
            return $"oid:{option.Id};oidx:{option.Index};content:{option.Content};pos:{fallbackPosition}";
        }

        private static SessionOperationResult Success(string message)
        {
            return new SessionOperationResult
            {
                Success = true,
                Message = message
            };
        }

        private async Task SendCurrentQuestionToParticipants(QuizSession session)
        {
            var participantIds = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == session.Id)
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var participantUserId in participantIds)
            {
                await SendCurrentQuestionToParticipant(session, participantUserId);
            }
        }

        private async Task SendCurrentQuestionToParticipant(QuizSession session, Guid userId)
        {
            var participant = await _dbContext.SessionParticipants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SessionId == session.Id && p.UserId == userId);

            if (participant is null || participant.CompletedAt.HasValue)
            {
                return;
            }

            var quiz = await _dbContext.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == session.QuizId);

            if (quiz is null)
            {
                return;
            }

            var questionIndex = participant.CurrentQuestionIndex;
            var settings = GetEffectiveQuizSettings(quiz);
            var orderedQuestions = GetOrderedQuestionsForParticipant(quiz, session.Id, userId, settings);
            var question = ResolveQuestionByProgress(orderedQuestions, questionIndex);
            if (question is null)
            {
                return;
            }

            var presentedOptions = GetPresentedOptionsForParticipant(
                question,
                session.Id,
                userId,
                questionIndex,
                settings);

            var presentedQuestion = new QuizQuestion
            {
                Id = question.Id,
                Index = question.Index,
                Content = question.Content,
                MediaUrl = question.MediaUrl,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                QuestionType = question.QuestionType,
                Options = presentedOptions
            };

            var mezonUserId = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == session.Id && p.UserId == userId)
                .Select(p => p.User.MezonUserId)
                .FirstOrDefaultAsync();

            if (!long.TryParse(mezonUserId, out var targetUserId) || targetUserId <= 0)
            {
                return;
            }

            if (ShouldSkipDuplicateQuestionDispatch(session.Id, userId, questionIndex))
            {
                return;
            }

            var content = QuizBotMessageFormatter.BuildQuestionMessageContent(session, quiz, presentedQuestion, questionIndex);
            var sendResult = await _mezonBotHostedService.SendDmMessageToUsersAsync([targetUserId], content);

            _logger.LogInformation(
                "DM question dispatch finished for session {SessionId}, user {UserId}, question {QuestionIndex}. Sent {SentCount}/{RequestedCount}.",
                session.Id,
                userId,
                questionIndex,
                sendResult.SentCount,
                sendResult.RequestedCount);
        }

        private static bool ShouldSkipDuplicateQuestionDispatch(Guid sessionId, Guid userId, int questionIndex)
        {
            var now = DateTime.UtcNow;
            var key = $"{sessionId:N}:{userId:N}:{questionIndex}";

            foreach (var item in RecentQuestionDispatches)
            {
                if (now - item.Value > QuestionDispatchDedupWindow)
                {
                    RecentQuestionDispatches.TryRemove(item.Key, out _);
                }
            }

            while (true)
            {
                if (RecentQuestionDispatches.TryGetValue(key, out var lastDispatch))
                {
                    if (now - lastDispatch <= QuestionDispatchDedupWindow)
                    {
                        return true;
                    }

                    if (RecentQuestionDispatches.TryUpdate(key, now, lastDispatch))
                    {
                        return false;
                    }

                    continue;
                }

                if (RecentQuestionDispatches.TryAdd(key, now))
                {
                    return false;
                }
            }
        }

        private static SessionOperationResult Fail(string message)
        {
            return new SessionOperationResult
            {
                Success = false,
                Message = message
            };
        }

        private async Task NotifySessionParticipantsAsync(QuizSession session, string title, string description, string color)
        {
            var mezonUserIds = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == session.Id)
                .Select(p => p.User.MezonUserId)
                .ToListAsync();

            var targetUserIds = mezonUserIds
                .Where(value => long.TryParse(value, out var parsed) && parsed > 0)
                .Select(value => long.Parse(value!))
                .Distinct()
                .ToList();

            if (targetUserIds.Count == 0)
            {
                return;
            }

            var content = QuizBotMessageFormatter.BuildSessionStatusMessageContent(title, description, color);
            var sendResult = await _mezonBotHostedService.SendDmMessageToUsersAsync(targetUserIds, content);

            _logger.LogInformation(
                "Session status notification dispatched. SessionId={SessionId}, Title={Title}, Sent={SentCount}/{RequestedCount}",
                session.Id,
                title,
                sendResult.SentCount,
                sendResult.RequestedCount);
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

            await _hubContext.Clients
                .Group($"quiz_{session.QuizId}")
                .SendAsync("SessionStateChanged", payload);
        }
    }
}
