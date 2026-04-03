using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Integration.Mezon;

namespace xUTest.Tests;

public class MezonWebhookTests
{
    [Fact]
    public async Task SendWebhookMessageAsync_WhenEnabled_SendsExpectedPayloadToClanWebhookEndpoint()
    {
        var captureHandler = new CaptureHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var httpClientFactory = new FixedHttpClientFactory(new HttpClient(captureHandler));
        var configuration = BuildConfig(enabled: true, token: "test-token");

        var service = new MezonBotHostedService(
            Mock.Of<IServiceScopeFactory>(),
            httpClientFactory,
            configuration,
            Mock.Of<ILogger<MezonBotHostedService>>());

        var sent = await service.SendWebhookMessageAsync("1843962578301095936", "Join thanh cong.");

        Assert.True(sent);
        Assert.Equal(HttpMethod.Post, captureHandler.LastMethod);
        Assert.Equal(
            "https://webhook.mezon.ai/clanwebhooks/test-token/1843962578301095936",
            captureHandler.LastUri);

        var body = captureHandler.LastBody;
        Assert.False(string.IsNullOrWhiteSpace(body));
        using var json = JsonDocument.Parse(body);

        var contentField = json.RootElement.GetProperty("content").GetString();
        Assert.False(string.IsNullOrWhiteSpace(contentField));

        using var contentJson = JsonDocument.Parse(contentField!);
        Assert.Equal("Join thanh cong.", contentJson.RootElement.GetProperty("t").GetString());

        var attachments = json.RootElement.GetProperty("attachments");
        Assert.Equal(JsonValueKind.Array, attachments.ValueKind);
        Assert.Empty(attachments.EnumerateArray());
    }

    [Fact]
    public async Task SendWebhookMessageAsync_WhenDisabled_DoesNotSendRequest()
    {
        var captureHandler = new CaptureHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK));

        var httpClientFactory = new FixedHttpClientFactory(new HttpClient(captureHandler));
        var configuration = BuildConfig(enabled: false, token: "test-token");

        var service = new MezonBotHostedService(
            Mock.Of<IServiceScopeFactory>(),
            httpClientFactory,
            configuration,
            Mock.Of<ILogger<MezonBotHostedService>>());

        var sent = await service.SendWebhookMessageAsync("1843962578301095936", "Ignored");

        Assert.False(sent);
        Assert.Equal(0, captureHandler.CallCount);
    }

    private static IConfiguration BuildConfig(bool enabled, string token)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MezonWebhook:Enabled"] = enabled.ToString(),
                ["MezonWebhook:ClanWebhookToken"] = token
            })
            .Build();
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class CaptureHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpMethod? LastMethod { get; private set; }

        public string? LastUri { get; private set; }

        public string? LastBody { get; private set; }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastMethod = request.Method;
            LastUri = request.RequestUri?.ToString();
            LastBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(_responseFactory(request));
        }
    }
}
