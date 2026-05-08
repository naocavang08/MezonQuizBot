using Mezon_sdk;
using Mezon_sdk.Models;
using WebApp.Integration.Mezon.Handlers;
using WebApp.Integration.Mezon.Services;
using WebApp.Integration.Mezon.Utils;

namespace WebApp.Integration.Mezon;

public sealed class MezonBotHostedService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MezonBotHostedService> _logger;
    private MezonClient? _client;

    private readonly MezonBotState _state;
    private readonly MezonUserService _userService;
    private readonly MezonMessageSender _messageSender;
    private readonly MezonCommandHandler _commandHandler;
    private readonly MezonButtonHandler _buttonHandler;

    public MezonBotHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MezonBotHostedService> logger,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _logger = logger;
        
        _state = new MezonBotState();
        _userService = new MezonUserService(loggerFactory.CreateLogger<MezonUserService>());
        _messageSender = new MezonMessageSender(_state, scopeFactory, loggerFactory.CreateLogger<MezonMessageSender>());
        
        var botId = (_configuration["MezonBot:BotId"] ?? string.Empty).Trim();
        
        _commandHandler = new MezonCommandHandler(scopeFactory, _messageSender, _userService, _state, botId, loggerFactory.CreateLogger<MezonCommandHandler>());
        _buttonHandler = new MezonButtonHandler(scopeFactory, _messageSender, _state, loggerFactory.CreateLogger<MezonButtonHandler>());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botId = (_configuration["MezonBot:BotId"] ?? string.Empty).Trim();
        var botToken = (_configuration["MezonBot:BotToken"] ?? string.Empty).Trim();
        var apiHost = (_configuration["MezonBot:ApiHost"] ?? "gw.mezon.ai").Trim();
        var apiPort = (_configuration["MezonBot:ApiPort"] ?? "443").Trim();
        var useSsl = !bool.TryParse(_configuration["MezonBot:UseSsl"], out var configuredUseSsl) || configuredUseSsl;

        if (string.IsNullOrWhiteSpace(botId) || string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Mezon bot config is missing BotId or BotToken. Hosted service is disabled.");
            return;
        }

        _client = new MezonClient(botId, botToken, host: apiHost, port: apiPort, useSsl: useSsl);
        _messageSender.SetClient(_client);

        _client.OnChannelMessage += _commandHandler.HandleChannelMessageAsync;
        _client.OnMessageButtonClicked += _buttonHandler.HandleButtonClickedAsync;

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
            _client.OnChannelMessage -= _commandHandler.HandleChannelMessageAsync;
            _client.OnMessageButtonClicked -= _buttonHandler.HandleButtonClickedAsync;

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

    public Task<BatchDmSendResult> SendDmMessageToUsersAsync(
        IEnumerable<long> userIds,
        ChannelMessageContent content,
        CancellationToken cancellationToken = default)
    {
        return _messageSender.SendDmMessageToUsersAsync(userIds, content, cancellationToken);
    }
}
