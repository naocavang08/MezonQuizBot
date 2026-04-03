using System.Net.Http;
using System.Net.Http.Headers;
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
using WebApp.Application.ManageQuizSession.Services;
using WebApp.Data;
using WebApp.Application.ManageQuizSession;
using PbChannelMessage = Mezon.Protobuf.ChannelMessage;

namespace WebApp.Integration.Mezon;

public sealed class MezonBotHostedService : BackgroundService
{
    private static readonly Regex JoinCommandRegex = new(
        @"^/join\s+([a-zA-Z0-9]{4,16})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MezonBotHostedService> _logger;

    private MezonClient? _client;
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

        var messageText = ExtractMessageText(message.Content);
        if (!TryParseJoinCode(messageText, out var code))
        {
            _logger.LogDebug(
                "Ignored message from sender {SenderId}. RawContent={RawContent}",
                senderId,
                message.Content);
            return;
        }

        _logger.LogInformation("Received join command from sender {SenderId} with code {SessionCode}.", senderId, code);

        SessionOperationResult operationResult;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quizSessionService = scope.ServiceProvider.GetRequiredService<IQuizSessionService>();

            var incomingUsername = (message.Username ?? string.Empty).Trim();
            var normalizedIncomingUsername = incomingUsername.ToLowerInvariant();

            var user = await dbContext.Users
                .AsNoTracking()
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
                    senderId,
                    "Cannot map Mezon sender to local user. Please login with Mezon on the web first and then try /join.");
                return;
            }

            operationResult = await quizSessionService.JoinByCode(code, new JoinQuizSessionDto
            {
                UserId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process /join command for sender {SenderId}.", senderId);
            await SendReplyAsync(message, senderId, "System is currently unavailable. Please try again later.");
            return;
        }

        var replyMessage = operationResult.Success
            ? $"Join successful for session {code}. {operationResult.Message}"
            : $"Join failed for session {code}. {operationResult.Message}";

        await SendReplyAsync(message, senderId, replyMessage);
    }

    private async Task SendReplyAsync(PbChannelMessage incomingMessage, string senderId, string message)
    {
        var sdkSent = await SendMessageViaSdkAsync(incomingMessage, message);
        if (sdkSent)
        {
            return;
        }

        await SendWebhookMessageAsync(senderId, message);
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

    public async Task<bool> SendWebhookMessageAsync(string userIdentifier, string message)
    {
        if (!_webhookEnabled)
        {
            _logger.LogDebug("Mezon webhook is disabled. Skip sending message to {UserIdentifier}.", userIdentifier);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_clanWebhookToken))
        {
            _logger.LogWarning("Mezon webhook token is empty. Cannot send message to {UserIdentifier}.", userIdentifier);
            return false;
        }

        var endpoint = $"https://webhook.mezon.ai/clanwebhooks/{Uri.EscapeDataString(_clanWebhookToken)}/{Uri.EscapeDataString(userIdentifier)}";

        var payload = new ClanWebhookRequest
        {
            Content = JsonSerializer.Serialize(new ClanWebhookContent
            {
                T = message
            })
        };

        var client = _httpClientFactory.CreateClient();
        var requestBody = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(requestBody, null, "application/json");

        using var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Mezon webhook send failed. Status={StatusCode} Body={ResponseBody}",
                (int)response.StatusCode,
                errorBody);
            return false;
        }

        var successBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "Mezon webhook message sent successfully to {UserIdentifier}. Response={ResponseBody}",
            userIdentifier,
            successBody);

        return true;
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
}
