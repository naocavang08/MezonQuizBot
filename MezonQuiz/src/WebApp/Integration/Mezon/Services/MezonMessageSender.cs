using Microsoft.EntityFrameworkCore;
using Mezon_sdk;
using Mezon_sdk.Constants;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;
using WebApp.Data;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Formatters;
using PbChannelMessage = Mezon.Net.Internal.Api.ChannelMessage;
using Rt = Mezon.Net.Internal.Realtime;
using WebApp.Integration.Mezon.Utils;

namespace WebApp.Integration.Mezon.Services;

public class MezonMessageSender
{
    private readonly MezonBotState _state;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MezonMessageSender> _logger;
    private MezonClient? _client;

    public MezonMessageSender(
        MezonBotState state,
        IServiceScopeFactory scopeFactory,
        ILogger<MezonMessageSender> logger)
    {
        _state = state;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void SetClient(MezonClient client)
    {
        _client = client;
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

    public async Task SendReplyAsync(PbChannelMessage incomingMessage, string message)
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

    public async Task SendReplyAsync(PbChannelMessage incomingMessage, ChannelMessageContent content)
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

    public async Task SendDmFeedbackAsync(string mezonUserId, ChannelMessageContent content)
    {
        if (!long.TryParse(mezonUserId, out var userId) || userId <= 0)
        {
            return;
        }

        await SendDmMessageToUserAsync(userId, content);
    }

    private async Task<bool> SendDmMessageToUserAsync(long userId, ChannelMessageContent content)
    {
        if (_client?.SocketManager is null || _client.ChannelManager is null)
        {
            _logger.LogWarning("Cannot send DM to user {UserId} because bot client is not connected.", userId);
            return false;
        }

        if (await ShouldSkipDuplicateOutboundMessageAsync(userId, content))
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

    private async Task<bool> ShouldSkipDuplicateOutboundMessageAsync(long userId, ChannelMessageContent content)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(content);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        var hashStr = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var key = $"msg:{userId}:{hashStr}";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.DedupRecords.Add(new Domain.Entites.DedupRecord
            {
                Key = key,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });
            await dbContext.SaveChangesAsync();
            return false;
        }
        catch (DbUpdateException)
        {
            return true;
        }
    }

    private async Task<bool> TrySendByKnownDmRouteAsync(long userId, ChannelMessageContent content)
    {
        if (_client?.SocketManager is null)
        {
            return false;
        }

        if (!_state.TryGetDmRoute(userId, out var route))
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

    public async Task TryLockAnsweredQuestionMessageAsync(
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
            && _state.TryGetDmRoute(senderAsLong, out var route)
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

    public async Task TryUpdateMultiChoiceQuestionMessageAsync(
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
            && _state.TryGetDmRoute(senderAsLong, out var route)
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

    public async Task SendParticipantFinishedQuizMessageAsync(
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
}

public sealed class BatchDmSendResult
{
    public int RequestedCount { get; init; }
    public int SentCount { get; init; }
    public List<long> FailedUserIds { get; init; } = [];
}

public sealed class ParticipantCompletionSummary
{
    public string QuizTitle { get; init; } = string.Empty;
    public int TotalScore { get; init; }
    public int CorrectCount { get; init; }
    public int AnswersCount { get; init; }
}
