using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mezon_sdk.Models;
using Moq;
using WebApp.Integration.Mezon;

namespace xUTest.Tests;

public class MezonWebhookTests
{
    [Fact]
    public async Task SendDmMessageToUsersAsync_WhenAllUserIdsAreInvalid_ReturnsEmptyResult()
    {
        var service = CreateService();

        var result = await service.SendDmMessageToUsersAsync(
            [0, -1, 0],
            new ChannelMessageContent { Text = "ignored" });

        Assert.Equal(0, result.RequestedCount);
        Assert.Equal(0, result.SentCount);
        Assert.Empty(result.FailedUserIds);
    }

    [Fact]
    public async Task SendDmMessageToUsersAsync_WhenClientIsNotConnected_FailsDistinctValidUsers()
    {
        var service = CreateService();

        var result = await service.SendDmMessageToUsersAsync(
            [11, 11, 22, 0, -3],
            new ChannelMessageContent { Text = "quiz update" });

        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(0, result.SentCount);
        Assert.Equal([11, 22], result.FailedUserIds);
    }

    private static MezonBotHostedService CreateService()
    {
        var configuration = new ConfigurationBuilder().Build();

        return new MezonBotHostedService(
            Mock.Of<IServiceScopeFactory>(),
            configuration,
            Mock.Of<ILogger<MezonBotHostedService>>());
    }
}
