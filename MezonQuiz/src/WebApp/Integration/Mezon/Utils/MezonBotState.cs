using System.Collections.Concurrent;
using System.Text.Json;
using Mezon_sdk.Models;
using PbChannelMessage = Mezon.Net.Internal.Api.ChannelMessage;
using WebApp.Application.ManageQuizSession.Dtos;
using Mezon_sdk.Utils;

namespace WebApp.Integration.Mezon.Utils;

public class MezonBotState
{
    private readonly ConcurrentDictionary<string, DateTime> _recentAnswerSubmissions = new();
    private readonly TimeSpan _answerSubmissionDedupWindow = TimeSpan.FromSeconds(3);
    
    private readonly ConcurrentDictionary<string, DateTime> _recentOutboundMessages = new();
    private readonly TimeSpan _outboundMessageDedupWindow = TimeSpan.FromSeconds(3);

    private readonly ConcurrentDictionary<long, DmRoute> _dmRoutes = new();
    private readonly ConcurrentDictionary<string, HashSet<int>> _pendingMultiChoiceSelections = new();

    public bool ShouldSkipDuplicateSubmission(Guid sessionId, Guid userId, int questionIndex)
    {
        var now = DateTime.UtcNow;
        var key = $"{sessionId:N}:{userId:N}:{questionIndex}";

        foreach (var item in _recentAnswerSubmissions)
        {
            if (now - item.Value > _answerSubmissionDedupWindow)
            {
                _recentAnswerSubmissions.TryRemove(item.Key, out _);
            }
        }

        if (_recentAnswerSubmissions.TryGetValue(key, out var lastSubmission)
            && now - lastSubmission <= _answerSubmissionDedupWindow)
        {
            return true;
        }

        _recentAnswerSubmissions[key] = now;
        return false;
    }

    public bool ShouldSkipDuplicateOutboundMessage(long userId, ChannelMessageContent content)
    {
        var now = DateTime.UtcNow;

        foreach (var item in _recentOutboundMessages)
        {
            if (now - item.Value > _outboundMessageDedupWindow)
            {
                _recentOutboundMessages.TryRemove(item.Key, out _);
            }
        }

        var payload = JsonSerializer.Serialize(content);
        var key = $"{userId}:{payload}";

        if (_recentOutboundMessages.TryGetValue(key, out var lastSent)
            && now - lastSent <= _outboundMessageDedupWindow)
        {
            return true;
        }

        _recentOutboundMessages[key] = now;
        return false;
    }

    public void CacheDmRoute(PbChannelMessage message)
    {
        if (message.ChannelId == 0 || message.SenderId == 0)
        {
            return;
        }

        var senderId = message.SenderId;
        var mode = Helper.ToInt(message.Mode)
            ?? Helper.ConvertChannelTypeToChannelMode((int)Mezon_sdk.Constants.ChannelType.ChannelTypeDm);

        _dmRoutes[(long)senderId] = new DmRoute
        {
            ChannelId = message.ChannelId,
            ClanId = message.ClanId,
            IsPublic = message.IsPublic,
            Mode = mode
        };
    }

    public bool TryGetDmRoute(long userId, out DmRoute route)
    {
        return _dmRoutes.TryGetValue(userId, out route!);
    }

    public string BuildMultiChoiceSelectionKey(Guid sessionId, int questionIndex, string mezonUserId)
    {
        return $"{sessionId:N}:{questionIndex}:{mezonUserId}";
    }

    public List<int> ToggleMultiChoiceSelection(string key, int selectedOption)
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

    public bool TryGetPendingMultiChoiceSelections(string key, out HashSet<int> pendingSelections)
    {
        return _pendingMultiChoiceSelections.TryGetValue(key, out pendingSelections!);
    }

    public void RemovePendingMultiChoiceSelections(string key)
    {
        _pendingMultiChoiceSelections.TryRemove(key, out _);
    }
}

public sealed class DmRoute
{
    public long ClanId { get; init; }
    public long ChannelId { get; init; }
    public bool IsPublic { get; init; }
    public int Mode { get; init; }
}
