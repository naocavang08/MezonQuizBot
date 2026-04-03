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
using WebApp.Integration.Mezon;

namespace WebApp.Application.ManageQuizSession.Services
{
    public class QuizSessionService : IQuizSessionService
    {
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
                    Code = s.Code ?? string.Empty,
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
                    Code = s.Code ?? string.Empty,
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
                JoinedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
            await BroadcastSessionStateChanged(session);
            return new SessionOperationResult { Success = true, Message = "Joined session successfully.", SessionId = session.Id };
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
            await SendCurrentQuestionToParticipants(session);
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
                QuestionType = question.QuestionType,
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

            var selectedOptions = ResolveSelectedOptions(question, request);
            if (selectedOptions.Count == 0)
            {
                return Fail("Selected option is invalid.");
            }

            var isCorrect = IsCorrectAnswer(question, selectedOptions);
            var points = isCorrect ? question.Points : 0;

            _dbContext.Answers.Add(new Answer
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = request.UserId,
                QuestionIndex = session.CurrentQuestion,
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
            var quiz = await _dbContext.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == session.QuizId);

            if (quiz is null)
            {
                _logger.LogWarning(
                    "Skip DM question dispatch for session {SessionId}: quiz not found.",
                    session.Id);
                return;
            }

            var question = ResolveQuestion(quiz, session.CurrentQuestion);
            if (question is null)
            {
                _logger.LogWarning(
                    "Skip DM question dispatch for session {SessionId}: question index {QuestionIndex} is invalid.",
                    session.Id,
                    session.CurrentQuestion);
                return;
            }

            var mezonUserIds = await _dbContext.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == session.Id)
                .Select(p => p.User.MezonUserId)
                .ToListAsync();

            var targets = mezonUserIds
                .Select(value => long.TryParse(value, out var parsedId) ? parsedId : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (targets.Count == 0)
            {
                _logger.LogInformation(
                    "Skip DM question dispatch for session {SessionId}: no participants with valid Mezon user id.",
                    session.Id);
                return;
            }

            var content = BuildQuestionMessageContent(session, quiz, question);
            var sendResult = await _mezonBotHostedService.SendDmMessageToUsersAsync(targets, content);

            _logger.LogInformation(
                "DM question dispatch finished for session {SessionId}. Sent {SentCount}/{RequestedCount}.",
                session.Id,
                sendResult.SentCount,
                sendResult.RequestedCount);
        }

        private static ChannelMessageContent BuildQuestionMessageContent(QuizSession session, Quiz quiz, QuizQuestion question)
        {
            var orderedOptions = (question.Options ?? new List<QuizOption>())
                .OrderBy(option => option.Index)
                .ToList();

            var hasZeroBasedIndex = orderedOptions.Any(option => option.Index == 0);
            var optionLines = orderedOptions
                .Select(option => $"{NormalizeOptionDisplayIndex(option.Index, hasZeroBasedIndex)} - {option.Content}")
                .ToList();

            var totalQuestionCount = quiz.Questions?.Count ?? 0;
            var title = $"[QUIZ] {quiz.Title} | Question {session.CurrentQuestion + 1}/{Math.Max(totalQuestionCount, 1)}";
            var optionsBlock = string.Join("\n", optionLines);
            var instruction = "(Reply with option number or answer on web if needed.)";

            var messageText = string.Join(
                "\n\n",
                new[]
                {
                    title,
                    question.Content,
                    optionsBlock,
                    instruction
                });

            var markdown = BuildMarkdownRanges(title, optionsBlock, messageText);

            return new ChannelMessageContent
            {
                Text = messageText,
                Markdown = markdown,
                Components = BuildOptionButtons(session, orderedOptions, hasZeroBasedIndex)
            };
        }

        private static List<MarkdownOnMessage> BuildMarkdownRanges(string title, string optionsBlock, string messageText)
        {
            var ranges = new List<MarkdownOnMessage>();

            if (!string.IsNullOrWhiteSpace(title))
            {
                ranges.Add(new MarkdownOnMessage
                {
                    Type = EMarkdownType.Bold,
                    Start = 0,
                    End = title.Length
                });
            }

            if (!string.IsNullOrWhiteSpace(optionsBlock))
            {
                var optionsStart = messageText.IndexOf(optionsBlock, StringComparison.Ordinal);
                if (optionsStart >= 0)
                {
                    ranges.Add(new MarkdownOnMessage
                    {
                        Type = EMarkdownType.Pre,
                        Start = optionsStart,
                        End = optionsStart + optionsBlock.Length
                    });
                }
            }

            return ranges;
        }

        private static List<MessageActionRow> BuildOptionButtons(
            QuizSession session,
            List<QuizOption> options,
            bool hasZeroBasedIndex)
        {
            if (options.Count == 0)
            {
                return [];
            }

            var buttonBuilder = new ButtonBuilder();
            foreach (var option in options)
            {
                var displayIndex = NormalizeOptionDisplayIndex(option.Index, hasZeroBasedIndex);
                var componentId = $"quiz:{session.Id}:q:{session.CurrentQuestion}:a:{displayIndex}";

                buttonBuilder.AddButton(
                    componentId: componentId,
                    label: displayIndex.ToString(),
                    style: ButtonMessageStyle.Primary);
            }

            var components = buttonBuilder
                .Build()
                .Select(component => new MessageComponent
                {
                    Type = component["type"],
                    ComponentId = component["id"].ToString(),
                    Component = component["component"] as Dictionary<string, object>
                })
                .ToList();

            return
            [
                new MessageActionRow
                {
                    Components = components
                }
            ];
        }

        private static int NormalizeOptionDisplayIndex(int optionIndex, bool hasZeroBasedIndex)
        {
            if (hasZeroBasedIndex)
            {
                return optionIndex + 1;
            }

            return optionIndex;
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
