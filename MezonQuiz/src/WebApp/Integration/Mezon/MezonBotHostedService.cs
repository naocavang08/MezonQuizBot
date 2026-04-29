using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mezon_sdk;
using Mezon_sdk.Constants;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Formatters;
using WebApp.Data;
using WebApp.Application.ManageQuizSession;
using PbChannelMessage = Mezon.Protobuf.ChannelMessage;
using Rt = Mezon.Protobuf.Realtime;
using WebApp.Application.ManageQuiz.Dtos;

namespace WebApp.Integration.Mezon;

public sealed class MezonBotHostedService : BackgroundService
{
    private static readonly Regex JoinCommandRegex = new(
        @"^/join\s+([a-zA-Z0-9]{4,16})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ExitCommandRegex = new(
        @"^/exit$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex LeaderboardCommandRegex = new(
        @"^/leaderboard$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex QuizButtonRegex = new(
        @"^quiz:([0-9a-fA-F\-]{36}):q:(\d+):a:(\d+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex QuizSubmitButtonRegex = new(
        @"^quiz:([0-9a-fA-F\-]{36}):q:(\d+):submit$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly ConcurrentDictionary<string, DateTime> RecentAnswerSubmissions = new();
    private static readonly TimeSpan AnswerSubmissionDedupWindow = TimeSpan.FromSeconds(3);
    private static readonly ConcurrentDictionary<string, DateTime> RecentOutboundMessages = new();
    private static readonly TimeSpan OutboundMessageDedupWindow = TimeSpan.FromSeconds(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MezonBotHostedService> _logger;

    private MezonClient? _client;
    private readonly ConcurrentDictionary<long, DmRoute> _dmRoutes = new();
    private readonly ConcurrentDictionary<string, HashSet<int>> _pendingMultiChoiceSelections = new();
    private string _botId = string.Empty;

    public MezonBotHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MezonBotHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botId = (_configuration["MezonBot:BotId"] ?? string.Empty).Trim();
        var botToken = (_configuration["MezonBot:BotToken"] ?? string.Empty).Trim();
        var apiHost = (_configuration["MezonBot:ApiHost"] ?? "gw.mezon.ai").Trim();
        var apiPort = (_configuration["MezonBot:ApiPort"] ?? "443").Trim();
        var useSsl = !bool.TryParse(_configuration["MezonBot:UseSsl"], out var configuredUseSsl) || configuredUseSsl;

        if (string.IsNullOrWhiteSpace(_botId) || string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Mezon bot config is missing BotId or BotToken. Hosted service is disabled.");
            return;
        }

        _client = new MezonClient(_botId, botToken, host: apiHost, port: apiPort, useSsl: useSsl);
        _client.OnChannelMessage += HandleChannelMessageAsync;
        _client.OnMessageButtonClicked += HandleButtonClickedAsync;

        try
        {
            _logger.LogInformation(
                "Starting Mezon bot with host={Host}, port={Port}, useSsl={UseSsl}.",
                apiHost,
                apiPort,
                useSsl);

            await _client.LoginAsync(enableAutoReconnect: true);
            _logger.LogInformation("Mezon bot connected and listening for /join command.");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mezon bot hosted service failed while connecting or listening.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            _client.OnChannelMessage -= HandleChannelMessageAsync;
            _client.OnMessageButtonClicked -= HandleButtonClickedAsync;

            try
            {
                await _client.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while disconnecting Mezon bot client.");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public async Task<BatchDmSendResult> SendDmMessageToUsersAsync(
        IEnumerable<long> userIds,
        ChannelMessageContent content,
        CancellationToken cancellationToken = default)
    {
        var uniqueUserIds = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (uniqueUserIds.Count == 0)
        {
            return new BatchDmSendResult();
        }

        var sentCount = 0;
        var failedUserIds = new List<long>();

        foreach (var userId in uniqueUserIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sent = await SendDmMessageToUserAsync(userId, content);
            if (sent)
            {
                sentCount += 1;
            }
            else
            {
                failedUserIds.Add(userId);
            }
        }

        return new BatchDmSendResult
        {
            RequestedCount = uniqueUserIds.Count,
            SentCount = sentCount,
            FailedUserIds = failedUserIds
        };
    }

    private async Task HandleChannelMessageAsync(PbChannelMessage message)
    {
        var senderId = message.SenderId.ToString();
        if (string.IsNullOrWhiteSpace(senderId) || senderId == "0")
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_botId) && string.Equals(senderId, _botId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CacheDmRoute(message);

        var messageText = ExtractMessageText(message.Content);
        var isExitCommand = IsExitCommand(messageText);
        var isLeaderboardCommand = IsLeaderboardCommand(messageText);
        var hasJoinCode = TryParseJoinCode(messageText, out var code);

        if (!isExitCommand && !isLeaderboardCommand && !hasJoinCode)
        {
            _logger.LogDebug(
                "Ignored message from sender {SenderId}. RawContent={RawContent}",
                senderId,
                message.Content);
            return;
        }

        if (isExitCommand)
        {
            _logger.LogInformation("Received exit command from sender {SenderId}.", senderId);
        }
        else if (isLeaderboardCommand)
        {
            _logger.LogInformation("Received leaderboard command from sender {SenderId}.", senderId);
        }
        else
        {
            _logger.LogInformation("Received join command from sender {SenderId} with code {SessionCode}.", senderId, code);
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quizSessionService = scope.ServiceProvider.GetRequiredService<IQuizSessionService>();

            var user = await ResolveOrCreateJoinUserAsync(dbContext, message, senderId);

            if (isExitCommand)
            {
                var operationResult = await quizSessionService.LeaveSessions(user.Id);
                var replyMessage = operationResult.Success
                    ? $"Leave successful. {operationResult.Message}"
                    : $"Leave failed. {operationResult.Message}";

                await SendReplyAsync(message, replyMessage);
                return;
            }

            if (isLeaderboardCommand)
            {
                var session = await quizSessionService.GetCurrentSessionForUser(user.Id);
                if (session is null)
                {
                    await SendReplyAsync(message, "Leaderboard unavailable. You are not in any current session.");
                    return;
                }

                var leaderboard = await quizSessionService.GetLeaderboard(session.Id);
                var leaderboardContent = QuizBotMessageFormatter.BuildLeaderboardMessageContent(session, leaderboard);
                await SendReplyAsync(message, leaderboardContent);
                return;
            }

            var joinResult = await quizSessionService.JoinByCode(code, new JoinQuizSessionDto
            {
                UserId = user.Id
            });

            var joinReplyMessage = joinResult.Success
                ? $"Join successful for session {code}. {joinResult.Message}"
                : $"Join failed for session {code}. {joinResult.Message}";

            await SendReplyAsync(message, joinReplyMessage);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process command for sender {SenderId}.", senderId);
            await SendReplyAsync(message, "System is currently unavailable. Please try again later.");
            return;
        }
    }

    private async Task HandleButtonClickedAsync(Rt.MessageButtonClicked clickEvent)
    {
        var buttonId = ExtractButtonId(clickEvent);
        var isSubmitAction = TryParseQuizSubmitButtonId(buttonId, out var submitSessionId, out var submitQuestionIndex);
        var isOptionAction = TryParseQuizButtonId(buttonId, out var optionSessionId, out var optionQuestionIndex, out var selectedOption);
        if (!isSubmitAction && !isOptionAction)
        {
            _logger.LogWarning(
                "Ignored button click because button id format is invalid. ButtonId={ButtonId}, RawButtonId={RawButtonId}, ExtraData={ExtraData}, SenderId={SenderId}, UserId={UserId}",
                buttonId,
                clickEvent.ButtonId,
                clickEvent.ExtraData,
                clickEvent.SenderId,
                clickEvent.UserId);
            return;
        }

        var sessionId = isSubmitAction ? submitSessionId : optionSessionId;
        var questionIndex = isSubmitAction ? submitQuestionIndex : optionQuestionIndex;

        var mezonUserId = ResolveMezonUserId(clickEvent);
        if (string.IsNullOrWhiteSpace(mezonUserId))
        {
            _logger.LogWarning(
                "Ignoring quiz button click with invalid sender. ButtonId={ButtonId}, SenderId={SenderId}, UserId={UserId}",
                buttonId,
                clickEvent.SenderId,
                clickEvent.UserId);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quizSessionService = scope.ServiceProvider.GetRequiredService<IQuizSessionService>();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.MezonUserId == mezonUserId);

            if (user is null)
            {
                var fallbackUsername = $"mezon_{mezonUserId}";
                user = await dbContext.SessionParticipants
                    .Where(p => p.SessionId == sessionId)
                    .Select(p => p.User)
                    .FirstOrDefaultAsync(u => u.Username == fallbackUsername);

                if (user is not null && string.IsNullOrWhiteSpace(user.MezonUserId))
                {
                    user.MezonUserId = mezonUserId;
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Linked fallback local user {UserId} with Mezon user id {SenderId} during button click.",
                        user.Id,
                        mezonUserId);
                }
            }

            if (user is null)
            {
                _logger.LogWarning(
                    "Cannot map Mezon button click sender to local user. SenderId={SenderId}, ButtonId={ButtonId}",
                    mezonUserId,
                    buttonId);
                return;
            }

            var currentQuestionResult = await quizSessionService.GetCurrentQuestion(sessionId, user.Id);
            if (!currentQuestionResult.Result.Success || currentQuestionResult.Question is null)
            {
                await SendDmFeedbackAsync(
                    mezonUserId,
                    QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("Current question is unavailable."));
                return;
            }

            var currentQuestion = currentQuestionResult.Question;
            if (currentQuestion.QuestionIndex != questionIndex)
            {
                await SendDmFeedbackAsync(
                    mezonUserId,
                    QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("Question has changed. Please answer the latest question."));
                return;
            }

            if (currentQuestion.QuestionType == QuestionType.MultipleChoice)
            {
                if (isSubmitAction)
                {
                    if (ShouldSkipDuplicateSubmission(sessionId, user.Id, questionIndex))
                    {
                        _logger.LogDebug(
                            "Skip duplicate multi-choice submission for session {SessionId}, user {UserId}, question {QuestionIndex}.",
                            sessionId,
                            user.Id,
                            questionIndex);
                        return;
                    }

                    var selectionKey = BuildMultiChoiceSelectionKey(sessionId, questionIndex, mezonUserId);
                    if (!_pendingMultiChoiceSelections.TryGetValue(selectionKey, out var pendingSelections)
                        || pendingSelections.Count == 0)
                    {
                        await SendDmFeedbackAsync(
                            mezonUserId,
                            QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("Please choose at least one option before submitting."));
                        return;
                    }

                    List<int> selectedIndexes;
                    lock (pendingSelections)
                    {
                        selectedIndexes = pendingSelections
                            .Distinct()
                            .OrderBy(index => index)
                            .ToList();
                    }

                    var multiChoiceSubmitResult = await quizSessionService.SubmitAnswer(sessionId, new SubmitAnswerDto
                    {
                        UserId = user.Id,
                        SelectedOption = selectedIndexes[0],
                        SelectedOptions = selectedIndexes,
                        SkipAutoDispatchNextQuestion = true
                    });

                    if (multiChoiceSubmitResult.Success)
                    {
                        _pendingMultiChoiceSelections.TryRemove(selectionKey, out _);
                    }

                    var multiChoiceFeedbackContent = QuizBotMessageFormatter.BuildAnswerFeedbackMessageContent(multiChoiceSubmitResult, questionIndex, selectedIndexes[0]);
                    await SendDmFeedbackAsync(mezonUserId, multiChoiceFeedbackContent);

                    var shouldLockMultiChoiceQuestionMessage = multiChoiceSubmitResult.Success
                        || multiChoiceSubmitResult.Message.Contains("already submitted", StringComparison.OrdinalIgnoreCase);

                    if (shouldLockMultiChoiceQuestionMessage)
                    {
                        await TryLockAnsweredQuestionMessageAsync(
                            clickEvent,
                            currentQuestion,
                            sessionId,
                            questionIndex,
                            mezonUserId);
                    }

                    if (multiChoiceSubmitResult.Success)
                    {
                        if (multiChoiceSubmitResult.ParticipantCompletedQuiz)
                        {
                            await SendParticipantFinishedQuizMessageAsync(mezonUserId, sessionId, user.Id, multiChoiceSubmitResult);
                        }
                        else
                        {
                            await quizSessionService.DispatchCurrentQuestionToParticipant(sessionId, user.Id);
                        }
                    }

                    return;
                }

                var toggledResolvedOption = await ResolveSubmittedOptionIndexAsync(
                    quizSessionService,
                    sessionId,
                    user.Id,
                    questionIndex,
                    selectedOption);

                var selectionKeyForToggle = BuildMultiChoiceSelectionKey(sessionId, questionIndex, mezonUserId);
                var selectedSnapshot = ToggleMultiChoiceSelection(selectionKeyForToggle, toggledResolvedOption);
                var selectedDisplays = selectedSnapshot
                    .Select(index => ResolveDisplayIndex(currentQuestion.Options, index))
                    .Where(index => index > 0)
                    .Distinct()
                    .OrderBy(index => index)
                    .ToList();

                await TryUpdateMultiChoiceQuestionMessageAsync(
                    clickEvent,
                    currentQuestion,
                    selectedDisplays,
                    mezonUserId);
                return;
            }

            if (ShouldSkipDuplicateSubmission(sessionId, user.Id, questionIndex))
            {
                _logger.LogDebug(
                    "Skip duplicate single-choice submission for session {SessionId}, user {UserId}, question {QuestionIndex}.",
                    sessionId,
                    user.Id,
                    questionIndex);
                return;
            }

            var resolvedOption = await ResolveSubmittedOptionIndexAsync(
                quizSessionService,
                sessionId,
                user.Id,
                questionIndex,
                selectedOption);

            var submitResult = await quizSessionService.SubmitAnswer(sessionId, new SubmitAnswerDto
            {
                UserId = user.Id,
                SelectedOption = resolvedOption,
                SelectedOptions = [resolvedOption],
                SkipAutoDispatchNextQuestion = true
            });

            var feedbackContent = QuizBotMessageFormatter.BuildAnswerFeedbackMessageContent(submitResult, questionIndex, selectedOption);
            await SendDmFeedbackAsync(mezonUserId, feedbackContent);

            var shouldLockQuestionMessage = submitResult.Success
                || submitResult.Message.Contains("already submitted", StringComparison.OrdinalIgnoreCase);

            if (shouldLockQuestionMessage)
            {
                await TryLockAnsweredQuestionMessageAsync(
                    clickEvent,
                    currentQuestion,
                    sessionId,
                    questionIndex,
                    mezonUserId);
            }

            if (submitResult.Success)
            {
                if (submitResult.ParticipantCompletedQuiz)
                {
                    await SendParticipantFinishedQuizMessageAsync(mezonUserId, sessionId, user.Id, submitResult);
                }
                else
                {
                    await quizSessionService.DispatchCurrentQuestionToParticipant(sessionId, user.Id);
                }
            }

            _logger.LogInformation(
                "Processed quiz button click. SessionId={SessionId}, QuestionIndex={QuestionIndex}, SelectedOption={SelectedOption}, SenderId={SenderId}, Success={Success}, Message={Message}",
                sessionId,
                questionIndex,
                resolvedOption,
                mezonUserId,
                submitResult.Success,
                submitResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process quiz button click. ButtonId={ButtonId}, SenderId={SenderId}",
                buttonId,
                mezonUserId);

            await SendDmFeedbackAsync(
                mezonUserId,
                QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("System is currently unavailable. Please try again."));
        }
    }

    private async Task SendReplyAsync(PbChannelMessage incomingMessage, string message)
    {
        var sdkSent = await SendMessageViaSdkAsync(
            incomingMessage,
            new ChannelMessageContent
            {
                Text = message
            });
        if (sdkSent)
        {
            return;
        }
    }

    private async Task SendReplyAsync(PbChannelMessage incomingMessage, ChannelMessageContent content)
    {
        var sdkSent = await SendMessageViaSdkAsync(incomingMessage, content);
        if (sdkSent)
        {
            return;
        }
    }

    private async Task<bool> SendMessageViaSdkAsync(PbChannelMessage incomingMessage, ChannelMessageContent content)
    {
        if (_client?.SocketManager is null)
        {
            return false;
        }

        if (incomingMessage.ChannelId == 0)
        {
            return false;
        }

        var mode = Helper.ToInt(incomingMessage.Mode)
            ?? Helper.ConvertChannelTypeToChannelMode((int)ChannelType.ChannelTypeDm);

        try
        {
            await _client.SocketManager.WriteChatMessageAsync(
                clanId: incomingMessage.ClanId,
                channelId: incomingMessage.ChannelId,
                mode: mode,
                isPublic: incomingMessage.IsPublic,
                content: content);

            _logger.LogInformation(
                "SDK reply sent to channel {ChannelId} for sender {SenderId}.",
                incomingMessage.ChannelId,
                incomingMessage.SenderId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "SDK reply failed for channel {ChannelId}. Falling back to webhook.",
                incomingMessage.ChannelId);

            return false;
        }
    }

    private async Task<Domain.Entites.User> ResolveOrCreateJoinUserAsync(AppDbContext dbContext, PbChannelMessage message, string senderId)
    {
        var incomingUsername = (message.Username ?? string.Empty).Trim();
        var normalizedIncomingUsername = incomingUsername.ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.MezonUserId == senderId ||
                (!string.IsNullOrWhiteSpace(incomingUsername) &&
                 u.Username.ToLower() == normalizedIncomingUsername));

        if (user is null)
        {
            var baseUsername = !string.IsNullOrWhiteSpace(incomingUsername)
                ? incomingUsername
                : $"mezon_{senderId}";

            var uniqueUsername = await GenerateUniqueUsernameAsync(dbContext, baseUsername);
            var now = DateTime.UtcNow;

            user = new Domain.Entites.User
            {
                MezonUserId = senderId,
                Username = uniqueUsername,
                DisplayName = string.IsNullOrWhiteSpace(message.DisplayName) ? null : message.DisplayName.Trim(),
                AvatarUrl = string.IsNullOrWhiteSpace(message.Avatar) ? null : message.Avatar.Trim(),
                IsActive = true,
                LastLoginAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Provisioned local user {UserId} for Mezon sender {SenderId} during /join.",
                user.Id,
                senderId);

            return user;
        }

        var hasChanges = false;
        if (string.IsNullOrWhiteSpace(user.MezonUserId))
        {
            user.MezonUserId = senderId;
            hasChanges = true;

            _logger.LogInformation(
                "Linked local user {UserId} with Mezon user id {SenderId} during /join.",
                user.Id,
                senderId);
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            hasChanges = true;
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        hasChanges = true;

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync();
        }

        return user;
    }

    private static async Task<string> GenerateUniqueUsernameAsync(AppDbContext dbContext, string baseUsername)
    {
        var sanitizedBase = string.IsNullOrWhiteSpace(baseUsername)
            ? "mezon_user"
            : baseUsername.Trim();

        if (sanitizedBase.Length > 255)
        {
            sanitizedBase = sanitizedBase.Substring(0, 255);
        }

        var uniqueUsername = sanitizedBase;
        var suffix = 1;

        while (await dbContext.Users.AnyAsync(u => u.Username == uniqueUsername))
        {
            var suffixText = $"_{suffix}";
            var maxBaseLength = Math.Max(1, 255 - suffixText.Length);
            var shortenedBase = sanitizedBase.Length > maxBaseLength
                ? sanitizedBase.Substring(0, maxBaseLength)
                : sanitizedBase;

            uniqueUsername = $"{shortenedBase}{suffixText}";
            suffix++;
        }

        return uniqueUsername;
    }

    private async Task<bool> SendDmMessageToUserAsync(long userId, ChannelMessageContent content)
    {
        if (_client?.SocketManager is null || _client.ChannelManager is null)
        {
            _logger.LogWarning("Cannot send DM to user {UserId} because bot client is not connected.", userId);
            return false;
        }

        if (ShouldSkipDuplicateOutboundMessage(userId, content))
        {
            _logger.LogDebug(
                "Skipped duplicate outbound DM for user {UserId}.",
                userId);
            return true;
        }

        try
        {
            if (await TrySendByKnownDmRouteAsync(userId, content))
            {
                return true;
            }

            var user = await _client.GetUserFromIdAsync(userId);
            await user.SendDmMessageAsync(content);

            _logger.LogInformation("DM question sent to Mezon user {UserId}.", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send DM question to Mezon user {UserId}.", userId);
            return false;
        }
    }

    private async Task SendDmFeedbackAsync(string mezonUserId, ChannelMessageContent content)
    {
        if (!long.TryParse(mezonUserId, out var userId) || userId <= 0)
        {
            return;
        }

        await SendDmMessageToUserAsync(userId, content);
    }

    private async Task SendParticipantFinishedQuizMessageAsync(
        string mezonUserId,
        Guid sessionId,
        Guid userId,
        SessionOperationResult? submitResult = null)
    {
        var summary = BuildParticipantCompletionSummary(submitResult)
            ?? await GetParticipantCompletionSummaryAsync(sessionId, userId);

        if (summary is null)
        {
            await SendDmFeedbackAsync(
                mezonUserId,
                QuizBotMessageFormatter.BuildParticipantFinishedQuizMessageContent(
                    "Quiz",
                    0,
                    0,
                    0));
            return;
        }

        await SendDmFeedbackAsync(
            mezonUserId,
            QuizBotMessageFormatter.BuildParticipantFinishedQuizMessageContent(
                summary.QuizTitle,
                summary.TotalScore,
                summary.CorrectCount,
                summary.AnswersCount));

        _logger.LogInformation(
            "Sent participant finished quiz message. SessionId={SessionId}, UserId={UserId}, QuizTitle={QuizTitle}, TotalScore={TotalScore}, CorrectCount={CorrectCount}, AnswersCount={AnswersCount}",
            sessionId,
            userId,
            summary.QuizTitle,
            summary.TotalScore,
            summary.CorrectCount,
            summary.AnswersCount);
    }

    private static ParticipantCompletionSummary? BuildParticipantCompletionSummary(SessionOperationResult? submitResult)
    {
        if (submitResult is null || !submitResult.Success)
        {
            return null;
        }

        var quizTitle = submitResult.QuizTitle?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(quizTitle)
            && submitResult.TotalScore is null
            && submitResult.CorrectCount is null
            && submitResult.AnswersCount is null)
        {
            return null;
        }

        return new ParticipantCompletionSummary
        {
            QuizTitle = string.IsNullOrWhiteSpace(quizTitle) ? "Quiz" : quizTitle,
            TotalScore = submitResult.TotalScore ?? 0,
            CorrectCount = submitResult.CorrectCount ?? 0,
            AnswersCount = submitResult.AnswersCount ?? 0
        };
    }

    private async Task<ParticipantCompletionSummary?> GetParticipantCompletionSummaryAsync(Guid sessionId, Guid userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.SessionParticipants
            .AsNoTracking()
            .Where(participant => participant.SessionId == sessionId && participant.UserId == userId)
            .Select(participant => new ParticipantCompletionSummary
            {
                QuizTitle = participant.Session.Quiz.Title,
                TotalScore = participant.TotalScore,
                CorrectCount = participant.CorrectCount,
                AnswersCount = participant.AnswersCount
            })
            .FirstOrDefaultAsync();
    }

    private async Task TryLockAnsweredQuestionMessageAsync(
        Rt.MessageButtonClicked clickEvent,
        QuizSessionQuestionDto answeredQuestion,
        Guid sessionId,
        int clickedQuestionIndex,
        string mezonUserId)
    {
        if (_client?.SocketManager is null)
        {
            return;
        }

        if (clickEvent.MessageId <= 0 || clickEvent.ChannelId <= 0)
        {
            return;
        }

        var content = QuizBotMessageFormatter.BuildAnsweredQuestionMessageContent(
            question: answeredQuestion,
            fallbackQuestionIndex: clickedQuestionIndex);

        var mode = Helper.ConvertChannelTypeToChannelMode((int)ChannelType.ChannelTypeDm);
        var clanId = 0L;
        var isPublic = false;

        if (long.TryParse(mezonUserId, out var senderAsLong)
            && _dmRoutes.TryGetValue(senderAsLong, out var route)
            && route.ChannelId == clickEvent.ChannelId)
        {
            mode = route.Mode;
            clanId = route.ClanId;
            isPublic = route.IsPublic;
        }

        try
        {
            await _client.SocketManager.UpdateChatMessageAsync(
                clanId: clanId,
                channelId: clickEvent.ChannelId,
                mode: mode,
                isPublic: isPublic,
                messageId: clickEvent.MessageId,
                content: content,
                hideEditted: true);

            _logger.LogInformation(
                "Locked answered question message. MessageId={MessageId}, ChannelId={ChannelId}, SessionId={SessionId}, QuestionIndex={QuestionIndex}",
                clickEvent.MessageId,
                clickEvent.ChannelId,
                sessionId,
                clickedQuestionIndex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to lock answered question message. MessageId={MessageId}, ChannelId={ChannelId}, SessionId={SessionId}",
                clickEvent.MessageId,
                clickEvent.ChannelId,
                sessionId);
        }
    }

    private async Task TryUpdateMultiChoiceQuestionMessageAsync(
        Rt.MessageButtonClicked clickEvent,
        QuizSessionQuestionDto question,
        List<int> selectedDisplays,
        string mezonUserId)
    {
        if (_client?.SocketManager is null)
        {
            return;
        }

        if (clickEvent.MessageId <= 0 || clickEvent.ChannelId <= 0)
        {
            return;
        }

        var mode = Helper.ConvertChannelTypeToChannelMode((int)ChannelType.ChannelTypeDm);
        var clanId = 0L;
        var isPublic = false;

        if (long.TryParse(mezonUserId, out var senderAsLong)
            && _dmRoutes.TryGetValue(senderAsLong, out var route)
            && route.ChannelId == clickEvent.ChannelId)
        {
            mode = route.Mode;
            clanId = route.ClanId;
            isPublic = route.IsPublic;
        }

        var content = QuizBotMessageFormatter.BuildMultiChoiceSelectionStateMessageContent(question, selectedDisplays);

        try
        {
            await _client.SocketManager.UpdateChatMessageAsync(
                clanId: clanId,
                channelId: clickEvent.ChannelId,
                mode: mode,
                isPublic: isPublic,
                messageId: clickEvent.MessageId,
                content: content,
                hideEditted: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to update multi-choice question message state. MessageId={MessageId}, ChannelId={ChannelId}, SessionId={SessionId}, QuestionIndex={QuestionIndex}",
                clickEvent.MessageId,
                clickEvent.ChannelId,
                question.SessionId,
                question.QuestionIndex);
        }
    }

    private void CacheDmRoute(PbChannelMessage message)
    {
        if (message.ChannelId == 0 || message.SenderId == 0)
        {
            return;
        }

        var senderId = message.SenderId;
        var mode = Helper.ToInt(message.Mode)
            ?? Helper.ConvertChannelTypeToChannelMode((int)ChannelType.ChannelTypeDm);

        _dmRoutes[(long)senderId] = new DmRoute
        {
            ChannelId = message.ChannelId,
            ClanId = message.ClanId,
            IsPublic = message.IsPublic,
            Mode = mode
        };
    }

    private async Task<bool> TrySendByKnownDmRouteAsync(long userId, ChannelMessageContent content)
    {
        if (_client?.SocketManager is null)
        {
            return false;
        }

        if (!_dmRoutes.TryGetValue(userId, out var route))
        {
            return false;
        }

        try
        {
            await _client.SocketManager.WriteChatMessageAsync(
                clanId: route.ClanId,
                channelId: route.ChannelId,
                mode: route.Mode,
                isPublic: route.IsPublic,
                content: content);

            _logger.LogInformation(
                "DM question sent to Mezon user {UserId} via cached route channel {ChannelId}.",
                userId,
                route.ChannelId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send DM via cached route for user {UserId} on channel {ChannelId}.",
                userId,
                route.ChannelId);

            return false;
        }
    }

    private static bool TryParseJoinCode(string input, out string code)
    {
        code = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = JoinCommandRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        code = match.Groups[1].Value.Trim().ToUpperInvariant();
        return code.Length > 0;
    }

    private static bool IsExitCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return ExitCommandRegex.IsMatch(input.Trim());
    }

    private static bool IsLeaderboardCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return LeaderboardCommandRegex.IsMatch(input.Trim());
    }

    private static bool TryParseQuizButtonId(string input, out Guid sessionId, out int questionIndex, out int selectedOption)
    {
        sessionId = Guid.Empty;
        questionIndex = -1;
        selectedOption = -1;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = QuizButtonRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!Guid.TryParse(match.Groups[1].Value, out sessionId))
        {
            return false;
        }

        if (!int.TryParse(match.Groups[2].Value, out questionIndex) || questionIndex < 0)
        {
            return false;
        }

        if (!int.TryParse(match.Groups[3].Value, out selectedOption) || selectedOption < 0)
        {
            return false;
        }

        return true;
    }

    private static bool TryParseQuizSubmitButtonId(string input, out Guid sessionId, out int questionIndex)
    {
        sessionId = Guid.Empty;
        questionIndex = -1;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = QuizSubmitButtonRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!Guid.TryParse(match.Groups[1].Value, out sessionId))
        {
            return false;
        }

        if (!int.TryParse(match.Groups[2].Value, out questionIndex) || questionIndex < 0)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldSkipDuplicateSubmission(Guid sessionId, Guid userId, int questionIndex)
    {
        var now = DateTime.UtcNow;
        var key = $"{sessionId:N}:{userId:N}:{questionIndex}";

        foreach (var item in RecentAnswerSubmissions)
        {
            if (now - item.Value > AnswerSubmissionDedupWindow)
            {
                RecentAnswerSubmissions.TryRemove(item.Key, out _);
            }
        }

        if (RecentAnswerSubmissions.TryGetValue(key, out var lastSubmission)
            && now - lastSubmission <= AnswerSubmissionDedupWindow)
        {
            return true;
        }

        RecentAnswerSubmissions[key] = now;
        return false;
    }

    private static bool ShouldSkipDuplicateOutboundMessage(long userId, ChannelMessageContent content)
    {
        var now = DateTime.UtcNow;

        foreach (var item in RecentOutboundMessages)
        {
            if (now - item.Value > OutboundMessageDedupWindow)
            {
                RecentOutboundMessages.TryRemove(item.Key, out _);
            }
        }

        var payload = JsonSerializer.Serialize(content);
        var key = $"{userId}:{payload}";

        if (RecentOutboundMessages.TryGetValue(key, out var lastSent)
            && now - lastSent <= OutboundMessageDedupWindow)
        {
            return true;
        }

        RecentOutboundMessages[key] = now;
        return false;
    }

    private static string BuildMultiChoiceSelectionKey(Guid sessionId, int questionIndex, string mezonUserId)
    {
        return $"{sessionId:N}:{questionIndex}:{mezonUserId}";
    }

    private List<int> ToggleMultiChoiceSelection(string key, int selectedOption)
    {
        var selections = _pendingMultiChoiceSelections.GetOrAdd(key, _ => new HashSet<int>());
        lock (selections)
        {
            if (!selections.Add(selectedOption))
            {
                selections.Remove(selectedOption);
            }

            return selections.OrderBy(index => index).ToList();
        }
    }

    private static string ResolveMezonUserId(Rt.MessageButtonClicked clickEvent)
    {
        if (clickEvent.UserId > 0)
        {
            return clickEvent.UserId.ToString();
        }

        return string.Empty;
    }

    private static string ExtractButtonId(Rt.MessageButtonClicked clickEvent)
    {
        var buttonId = (clickEvent.ButtonId ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(buttonId))
        {
            return buttonId;
        }

        var extraData = (clickEvent.ExtraData ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(extraData))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(extraData);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var candidate = TryGetStringProperty(doc.RootElement, "button_id")
                    ?? TryGetStringProperty(doc.RootElement, "buttonId")
                    ?? TryGetStringProperty(doc.RootElement, "id")
                    ?? TryGetStringProperty(doc.RootElement, "component_id")
                    ?? TryGetStringProperty(doc.RootElement, "componentId");

                return candidate?.Trim() ?? string.Empty;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return (doc.RootElement.GetString() ?? string.Empty).Trim();
            }
        }
        catch (JsonException)
        {
            // Keep fallback behavior below for non-JSON extra_data.
        }

        return extraData;
    }

    private static string? TryGetStringProperty(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.String => node.GetString(),
            JsonValueKind.Number => node.ToString(),
            _ => null
        };
    }

    private static async Task<int> ResolveSubmittedOptionIndexAsync(
        IQuizSessionService quizSessionService,
        Guid sessionId,
        Guid userId,
        int clickedQuestionIndex,
        int selectedOption)
    {
        var currentQuestion = await quizSessionService.GetCurrentQuestion(sessionId, userId);
        if (!currentQuestion.Result.Success || currentQuestion.Question is null)
        {
            return selectedOption;
        }

        if (currentQuestion.Question.QuestionIndex != clickedQuestionIndex)
        {
            return selectedOption;
        }

        var options = currentQuestion.Question.Options ?? [];
        if (selectedOption > 0 && selectedOption <= options.Count)
        {
            return options[selectedOption - 1].Index;
        }

        if (options.Any(option => option.Index == selectedOption))
        {
            return selectedOption;
        }

        return selectedOption;
    }

    private static int ResolveDisplayIndex(IReadOnlyList<QuizSessionQuestionOptionDto> options, int optionIndex)
    {
        for (var position = 0; position < options.Count; position++)
        {
            if (options[position].Index == optionIndex)
            {
                return position + 1;
            }
        }

        return -1;
    }

    private static string ExtractMessageText(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return string.Empty;
        }

        var trimmed = rawContent.Trim();

        try
        {
            using var document = JsonDocument.Parse(trimmed);

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (document.RootElement.TryGetProperty("t", out var tNode) && tNode.ValueKind == JsonValueKind.String)
                {
                    return tNode.GetString() ?? string.Empty;
                }

                if (document.RootElement.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
                {
                    return textNode.GetString() ?? string.Empty;
                }
            }

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return trimmed;
    }

    public sealed class BatchDmSendResult
    {
        public int RequestedCount { get; init; }
        public int SentCount { get; init; }
        public List<long> FailedUserIds { get; init; } = [];
    }

    private sealed class DmRoute
    {
        public long ClanId { get; init; }
        public long ChannelId { get; init; }
        public bool IsPublic { get; init; }
        public int Mode { get; init; }
    }

    private sealed class ParticipantCompletionSummary
    {
        public string QuizTitle { get; init; } = string.Empty;
        public int TotalScore { get; init; }
        public int CorrectCount { get; init; }
        public int AnswersCount { get; init; }
    }
}
