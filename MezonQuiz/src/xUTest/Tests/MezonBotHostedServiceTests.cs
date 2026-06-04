using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Application.ManageQuizSession;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Data;
using WebApp.Integration.Mezon;
using WebApp.Integration.Mezon.Handlers;
using PbChannelMessage = Mezon.Net.Internal.Api.ChannelMessage;

namespace xUTest.Tests;

public sealed class MezonBotHostedServiceTests
{
    [Fact]
    public async Task HandleChannelMessageAsync_WhenJoinCommandIsReceived_CreatesUserAndCallsJoinByCode()
    {
        await using var harness = await CreateHarnessAsync();
        harness.QuizSessionService
            .Setup(service => service.JoinByCodeFromBot(
                "ABCD1234",
                It.Is<JoinQuizSessionDto>(dto => dto.UserId != Guid.Empty)))
            .ReturnsAsync(new SessionOperationResult
            {
                Success = true,
                Message = "Joined session."
            });

        var service = harness.CreateService();

        await InvokeHandleChannelMessageAsync(service, new PbChannelMessage
        {
            SenderId = 1843962578301095936,
            Username = "quiz-user",
            DisplayName = "Quiz User",
            Avatar = "https://cdn.test/avatar.png",
            Content = "{\"t\":\"/join abcd1234\"}"
        });

        harness.QuizSessionService.Verify(
            svc => svc.JoinByCodeFromBot(
                "ABCD1234",
                It.Is<JoinQuizSessionDto>(dto => dto.UserId != Guid.Empty)),
            Times.Once);

        var createdUser = await harness.DbContext.Users.SingleAsync();
        Assert.Equal("1843962578301095936", createdUser.MezonUserId);
        Assert.Equal("quiz-user", createdUser.Username);
        Assert.Equal("Quiz User", createdUser.DisplayName);
    }

    [Fact]
    public async Task HandleChannelMessageAsync_WhenMessageIsNotSupportedCommand_DoesNotReachQuizSessionService()
    {
        await using var harness = await CreateHarnessAsync();
        var service = harness.CreateService();

        await InvokeHandleChannelMessageAsync(service, new PbChannelMessage
        {
            SenderId = 1843962578301095936,
            Username = "quiz-user",
            Content = "{\"t\":\"hello bot\"}"
        });

        harness.QuizSessionService.Verify(
            svc => svc.JoinByCodeFromBot(It.IsAny<string>(), It.IsAny<JoinQuizSessionDto>()),
            Times.Never);
        harness.QuizSessionService.Verify(
            svc => svc.LeaveSessions(It.IsAny<Guid>()),
            Times.Never);
        harness.QuizSessionService.Verify(
            svc => svc.GetCurrentSessionForUser(It.IsAny<Guid>()),
            Times.Never);
        Assert.Empty(await harness.DbContext.Users.ToListAsync());
    }

    private static async Task<TestHarness> CreateHarnessAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddScoped(_ =>
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            return new AppDbContext(options);
        });

        var quizSessionService = new Mock<IQuizSessionService>(MockBehavior.Strict);
        services.AddScoped(_ => quizSessionService.Object);

        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        return new TestHarness(
            connection,
            provider,
            provider.GetRequiredService<IServiceScopeFactory>(),
            dbContext,
            quizSessionService);
    }

    private static async Task InvokeHandleChannelMessageAsync(MezonBotHostedService service, PbChannelMessage message)
    {
        var field = typeof(MezonBotHostedService).GetField(
            "_commandHandler",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        var handler = field!.GetValue(service) as MezonCommandHandler;
        Assert.NotNull(handler);
        await handler!.HandleChannelMessageAsync(message);
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        public TestHarness(
            SqliteConnection connection,
            ServiceProvider provider,
            IServiceScopeFactory scopeFactory,
            AppDbContext dbContext,
            Mock<IQuizSessionService> quizSessionService)
        {
            Connection = connection;
            Provider = provider;
            ScopeFactory = scopeFactory;
            DbContext = dbContext;
            QuizSessionService = quizSessionService;
        }

        public SqliteConnection Connection { get; }

        public ServiceProvider Provider { get; }

        public IServiceScopeFactory ScopeFactory { get; }

        public AppDbContext DbContext { get; }

        public Mock<IQuizSessionService> QuizSessionService { get; }

        public MezonBotHostedService CreateService()
        {
            return new MezonBotHostedService(
                ScopeFactory,
                new ConfigurationBuilder().Build(),
                Mock.Of<ILogger<MezonBotHostedService>>(),
                LoggerFactory.Create(_ => { }));
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
            await Provider.DisposeAsync();
        }
    }
}
