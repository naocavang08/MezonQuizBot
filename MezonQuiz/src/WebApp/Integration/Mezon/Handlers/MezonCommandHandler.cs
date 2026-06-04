using WebApp.Data;
using WebApp.Application.ManageQuizSession;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Formatters;
using PbChannelMessage = Mezon.Net.Internal.Api.ChannelMessage;
using WebApp.Integration.Mezon.Services;
using WebApp.Integration.Mezon.Utils;

namespace WebApp.Integration.Mezon.Handlers;

public class MezonCommandHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MezonMessageSender _messageSender;
    private readonly MezonUserService _userService;
    private readonly MezonBotState _state;
    private readonly string _botId;
    private readonly ILogger<MezonCommandHandler> _logger;

    public MezonCommandHandler(
        IServiceScopeFactory scopeFactory,
        MezonMessageSender messageSender,
        MezonUserService userService,
        MezonBotState state,
        string botId,
        ILogger<MezonCommandHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _messageSender = messageSender;
        _userService = userService;
        _state = state;
        _botId = botId;
        _logger = logger;
    }

    public async Task HandleChannelMessageAsync(PbChannelMessage message)
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

        _state.CacheDmRoute(message);

        var messageText = MezonBotParser.ExtractMessageText(message.Content);
        var isExitCommand = MezonBotParser.IsExitCommand(messageText);
        var isLeaderboardCommand = MezonBotParser.IsLeaderboardCommand(messageText);
        var hasJoinCode = MezonBotParser.TryParseJoinCode(messageText, out var code);

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

            var user = await _userService.ResolveOrCreateJoinUserAsync(dbContext, message, senderId);

            if (isExitCommand)
            {
                var operationResult = await quizSessionService.LeaveSessions(user.Id);
                var replyMessage = operationResult.Success
                    ? $"Leave successful. {operationResult.Message}"
                    : $"Leave failed. {operationResult.Message}";

                await _messageSender.SendReplyAsync(message, replyMessage);
                return;
            }

            if (isLeaderboardCommand)
            {
                var session = await quizSessionService.GetCurrentSessionForUser(user.Id);
                if (session is null)
                {
                    await _messageSender.SendReplyAsync(message, "Leaderboard unavailable. You are not in any current session.");
                    return;
                }

                var leaderboard = await quizSessionService.GetLeaderboard(session.Id);
                var leaderboardContent = QuizBotMessageFormatter.BuildLeaderboardMessageContent(session, leaderboard);
                await _messageSender.SendReplyAsync(message, leaderboardContent);
                return;
            }

            var joinResult = await quizSessionService.JoinByCodeFromBot(code, new JoinQuizSessionDto
            {
                UserId = user.Id
            });

            var joinReplyMessage = joinResult.Success
                ? $"Join successful for session {code}. {joinResult.Message}"
                : $"Join failed for session {code}. {joinResult.Message}";

            await _messageSender.SendReplyAsync(message, joinReplyMessage);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process command for sender {SenderId}.", senderId);
            await _messageSender.SendReplyAsync(message, "System is currently unavailable. Please try again later.");
            return;
        }
    }
}
