using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mezon.Protobuf;
using Mezon_sdk;
using Mezon_sdk.Constants;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Formatters;
using WebApp.Application.ManageQuizSession.Services;
using WebApp.Data;
using WebApp.Application.ManageQuizSession;
using PbChannelMessage = Mezon.Protobuf.ChannelMessage;
using Rt = Mezon.Protobuf.Realtime;

namespace WebApp.Integration.Mezon;

public sealed class MezonBotHostedService : BackgroundService
{
    private static readonly Regex JoinCommandRegex = new(
        @"^/join\s+([a-zA-Z0-9]{4,16})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ExitCommandRegex = new(
        @"^/exit$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex QuizButtonRegex = new(
        @"^quiz:([0-9a-fA-F\-]{36}):q:(\d+):a:(\d+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MezonBotHostedService> _logger;

    private MezonClient? _client;
    private readonly ConcurrentDictionary<long, DmRoute> _dmRoutes = new();
    private string _botId = string.Empty;
    private string _clanWebhookToken = string.Empty;
    private bool _webhookEnabled;

    public MezonBotHostedService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MezonBotHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        _clanWebhookToken = (_configuration["MezonWebhook:ClanWebhookToken"] ?? string.Empty).Trim();
        _webhookEnabled = bool.TryParse(_configuration["MezonWebhook:Enabled"], out var enabled) && enabled;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botId = (_configuration["MezonBot:BotId"] ?? string.Empty).Trim();
        var botToken = (_configuration["MezonBot:BotToken"] ?? string.Empty).Trim();
        var apiHost = (_configuration["MezonBot:ApiHost"] ?? "gw.mezon.ai").Trim();
        var apiPort = (_configuration["MezonBot:ApiPort"] ?? "443").Trim();
        var useSsl = !bool.TryParse(_configuration["MezonBot:UseSsl"], out var configuredUseSsl) || configuredUseSsl;

        _clanWebhookToken = (_configuration["MezonWebhook:ClanWebhookToken"] ?? string.Empty).Trim();
        _webhookEnabled = bool.TryParse(_configuration["MezonWebhook:Enabled"], out var enabled) && enabled;

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
        var hasJoinCode = TryParseJoinCode(messageText, out var code);

        if (!isExitCommand && !hasJoinCode)
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
        else
        {
            _logger.LogInformation("Received join command from sender {SenderId} with code {SessionCode}.", senderId, code);
        }

        SessionOperationResult operationResult;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quizSessionService = scope.ServiceProvider.GetRequiredService<IQuizSessionService>();

            var incomingUsername = (message.Username ?? string.Empty).Trim();
            var normalizedIncomingUsername = incomingUsername.ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.MezonUserId == senderId ||
                    (!string.IsNullOrWhiteSpace(incomingUsername) &&
                     u.Username.ToLower() == normalizedIncomingUsername));

            if (user is null)
            {
                _logger.LogWarning(
                    "Cannot map Mezon sender to local user. SenderId={SenderId}, Username={Username}",
                    senderId,
                    incomingUsername);

                await SendReplyAsync(
                    message,
                    "Cannot map Mezon sender to local user. Please login with Mezon on the web first and then try /join.");
                return;
            }

            if (string.IsNullOrWhiteSpace(user.MezonUserId))
            {
                user.MezonUserId = senderId;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Linked local user {UserId} with Mezon user id {SenderId} during /join.",
                    user.Id,
                    senderId);
            }

            if (isExitCommand)
            {
                operationResult = await quizSessionService.LeaveSessions(user.Id);
            }
            else
            {
                operationResult = await quizSessionService.JoinByCode(code, new JoinQuizSessionDto
                {
                    UserId = user.Id
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process command for sender {SenderId}.", senderId);
            await SendReplyAsync(message, "System is currently unavailable. Please try again later.");
            return;
        }

        var replyMessage = isExitCommand
            ? (operationResult.Success
                ? $"Leave successful. {operationResult.Message}"
                : $"Leave failed. {operationResult.Message}")
            : (operationResult.Success
                ? $"Join successful for session {code}. {operationResult.Message}"
                : $"Join failed for session {code}. {operationResult.Message}");

        await SendReplyAsync(message, replyMessage);
    }

    private async Task HandleButtonClickedAsync(Rt.MessageButtonClicked clickEvent)
    {
        var buttonId = ExtractButtonId(clickEvent);
        if (!TryParseQuizButtonId(buttonId, out var sessionId, out var questionIndex, out var selectedOption))
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

            var resolvedOption = await ResolveSubmittedOptionIndexAsync(
                quizSessionService,
                sessionId,
                questionIndex,
                selectedOption);

            var submitResult = await quizSessionService.SubmitAnswer(sessionId, new SubmitAnswerDto
            {
                UserId = user.Id,
                SelectedOption = resolvedOption,
                SelectedOptions = [resolvedOption]
            });

            var feedbackContent = QuizBotMessageFormatter.BuildAnswerFeedbackMessageContent(submitResult, questionIndex, selectedOption);
            await SendDmFeedbackAsync(mezonUserId, feedbackContent);

            var shouldLockQuestionMessage = submitResult.Success
                || submitResult.Message.Contains("already submitted", StringComparison.OrdinalIgnoreCase);

            if (shouldLockQuestionMessage)
            {
                await TryLockAnsweredQuestionMessageAsync(
                    clickEvent,
                    quizSessionService,
                    sessionId,
                    questionIndex,
                    mezonUserId);
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
        var sdkSent = await SendMessageViaSdkAsync(incomingMessage, message);
        if (sdkSent)
        {
            return;
        }
    }

    private async Task<bool> SendMessageViaSdkAsync(PbChannelMessage incomingMessage, string message)
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
                content: new ChannelMessageContent
                {
                    Text = message
                });

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

    private async Task<bool> SendDmMessageToUserAsync(long userId, ChannelMessageContent content)
    {
        if (_client?.SocketManager is null || _client.ChannelManager is null)
        {
            _logger.LogWarning("Cannot send DM to user {UserId} because bot client is not connected.", userId);
            return false;
        }

        try
        {
            if (await TrySendByKnownDmRouteAsync(userId, content))
            {
                return true;
            }

            if (userId > int.MaxValue)
            {
                _logger.LogWarning(
                    "Cannot create DM channel via SDK for user {UserId} because ID exceeds Int32. User should message bot first to establish DM route.",
                    userId);
                return false;
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

    private async Task TryLockAnsweredQuestionMessageAsync(
        Rt.MessageButtonClicked clickEvent,
        IQuizSessionService quizSessionService,
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

        var content = await BuildAnsweredQuestionMessageContentAsync(
            quizSessionService,
            sessionId,
            clickedQuestionIndex);

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

    private static async Task<ChannelMessageContent> BuildAnsweredQuestionMessageContentAsync(
        IQuizSessionService quizSessionService,
        Guid sessionId,
        int clickedQuestionIndex)
    {
        var currentQuestion = await quizSessionService.GetCurrentQuestion(sessionId);
        if (!currentQuestion.Result.Success || currentQuestion.Question is null)
        {
            return QuizBotMessageFormatter.BuildAnsweredQuestionMessageContent(
                question: null,
                fallbackQuestionIndex: clickedQuestionIndex);
        }

        if (currentQuestion.Question.QuestionIndex != clickedQuestionIndex)
        {
            return QuizBotMessageFormatter.BuildAnsweredQuestionMessageContent(
                question: null,
                fallbackQuestionIndex: clickedQuestionIndex);
        }

        return QuizBotMessageFormatter.BuildAnsweredQuestionMessageContent(
            question: currentQuestion.Question,
            fallbackQuestionIndex: clickedQuestionIndex);
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
        int clickedQuestionIndex,
        int selectedOption)
    {
        var currentQuestion = await quizSessionService.GetCurrentQuestion(sessionId);
        if (!currentQuestion.Result.Success || currentQuestion.Question is null)
        {
            return selectedOption;
        }

        if (currentQuestion.Question.QuestionIndex != clickedQuestionIndex)
        {
            return selectedOption;
        }

        var options = currentQuestion.Question.Options ?? [];
        var hasZeroBasedOption = options.Any(option => option.Index == 0);
        if (hasZeroBasedOption && selectedOption > 0)
        {
            return selectedOption - 1;
        }

        if (options.Any(option => option.Index == selectedOption))
        {
            return selectedOption;
        }

        return selectedOption;
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

    private sealed class ClanWebhookRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<ClanWebhookAttachment> Attachments { get; init; } = [];
    }

    private sealed class ClanWebhookContent
    {
        [JsonPropertyName("t")]
        public string T { get; init; } = string.Empty;
    }

    private sealed class ClanWebhookAttachment
    {
        [JsonPropertyName("url")]
        public string Url { get; init; } = string.Empty;

        [JsonPropertyName("filetype")]
        public string Filetype { get; init; } = string.Empty;
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
}
