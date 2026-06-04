using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Application.Auth.Authorization;
using WebApp.Application.Categories.Dtos;
using WebApp.Application.Categories.Services;
using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Application.ManageQuiz.Services;
using WebApp.Application.ManageQuizSession;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Services;
using WebApp.Data;
using WebApp.Domain.Entites;
using WebApp.Integration.Mezon;
using WebApp.Realtime;
using static WebApp.Domain.Enums.Status;

namespace xUTest.Tests;

public sealed class LogicRegressionTests
{
    [Fact]
    public async Task AddQuestion_WhenUserDoesNotOwnQuiz_ThrowsUnauthorized()
    {
        await using var harness = await DbHarness.CreateAsync();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var quizId = Guid.NewGuid();

        harness.DbContext.Users.AddRange(
            CreateUser(ownerId, "owner"),
            CreateUser(otherUserId, "creator-two"));
        harness.DbContext.Quizzes.Add(CreateQuiz(quizId, ownerId));
        await harness.DbContext.SaveChangesAsync();

        var service = CreateQuizService(harness.DbContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AddQuestion(otherUserId, quizId, CreateQuestion()));
    }

    [Fact]
    public async Task AddQuestion_WhenUserHasSystemRole_AllowsCrossOwnerMutation()
    {
        await using var harness = await DbHarness.CreateAsync();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var quizId = Guid.NewGuid();

        harness.DbContext.Users.AddRange(
            CreateUser(ownerId, "owner"),
            CreateUser(adminId, "admin"));
        harness.DbContext.Roles.Add(new Role { Id = roleId, Name = "super_admin", IsSystem = true });
        harness.DbContext.UserRoles.Add(new UserRole { UserId = adminId, RoleId = roleId });
        harness.DbContext.Quizzes.Add(CreateQuiz(quizId, ownerId));
        await harness.DbContext.SaveChangesAsync();

        var service = CreateQuizService(harness.DbContext);

        await service.AddQuestion(adminId, quizId, CreateQuestion());

        var quiz = await harness.DbContext.Quizzes.AsNoTracking().SingleAsync(q => q.Id == quizId);
        Assert.Single(quiz.Questions);
    }

    [Fact]
    public async Task JoinByCodeForUser_IgnoresBodyUserIdAndJoinsCurrentUser()
    {
        await using var harness = await DbHarness.CreateAsync();
        var hostId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var forgedUserId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        harness.DbContext.Users.AddRange(
            CreateUser(hostId, "host"),
            CreateUser(currentUserId, "current"),
            CreateUser(forgedUserId, "forged"));
        harness.DbContext.Quizzes.Add(CreateQuiz(quizId, hostId));
        harness.DbContext.QuizSessions.Add(new QuizSession
        {
            Id = sessionId,
            QuizId = quizId,
            HostId = hostId,
            Code = "ABC123",
            Status = SessionStatus.Waiting
        });
        await harness.DbContext.SaveChangesAsync();

        var service = CreateQuizSessionService(harness.DbContext);

        var result = await service.JoinByCodeForUser("ABC123", currentUserId);

        Assert.True(result.Success);
        Assert.True(await harness.DbContext.SessionParticipants.AnyAsync(p => p.SessionId == sessionId && p.UserId == currentUserId));
        Assert.False(await harness.DbContext.SessionParticipants.AnyAsync(p => p.SessionId == sessionId && p.UserId == forgedUserId));
    }

    [Fact]
    public async Task SubmitAnswerForUser_OverridesForgedBodyUserId()
    {
        await using var harness = await DbHarness.CreateAsync();
        var hostId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var forgedUserId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        harness.DbContext.Users.AddRange(
            CreateUser(hostId, "host"),
            CreateUser(currentUserId, "current"),
            CreateUser(forgedUserId, "forged"));
        harness.DbContext.Quizzes.Add(CreateQuiz(quizId, hostId, [CreateQuestion()]));
        harness.DbContext.QuizSessions.Add(new QuizSession
        {
            Id = sessionId,
            QuizId = quizId,
            HostId = hostId,
            Code = "ABC123",
            Status = SessionStatus.Active
        });
        harness.DbContext.SessionParticipants.Add(new SessionParticipant
        {
            SessionId = sessionId,
            UserId = currentUserId
        });
        await harness.DbContext.SaveChangesAsync();

        var service = CreateQuizSessionService(harness.DbContext);

        var result = await service.SubmitAnswerForUser(sessionId, currentUserId, new SubmitAnswerDto
        {
            UserId = forgedUserId,
            SelectedOption = 0,
            SelectedOptions = [0],
            SkipAutoDispatchNextQuestion = true
        });

        Assert.True(result.Success);
        Assert.True(await harness.DbContext.Answers.AnyAsync(a => a.SessionId == sessionId && a.UserId == currentUserId));
        Assert.False(await harness.DbContext.Answers.AnyAsync(a => a.SessionId == sessionId && a.UserId == forgedUserId));
    }

    [Fact]
    public async Task CategoryCreateAndUpdate_HandleNullSlugAndRejectDuplicateName()
    {
        await using var harness = await DbHarness.CreateAsync();
        var service = new CategoryService(harness.DbContext);

        var first = await service.CreateCategoryAsync(new SaveCategoryDto
        {
            Name = " General ",
            Slug = null
        });

        Assert.Equal("General", first.Name);
        Assert.Null(first.Slug);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCategoryAsync(new SaveCategoryDto
            {
                Name = "general",
                Slug = null
            }));

        var second = await service.CreateCategoryAsync(new SaveCategoryDto
        {
            Name = "Science",
            Slug = "science"
        });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateCategoryAsync(second.Id, new SaveCategoryDto
            {
                Name = "General",
                Slug = null
            }));
    }

    private static QuizService CreateQuizService(AppDbContext dbContext)
    {
        return new QuizService(
            dbContext,
            Mock.Of<IWebHostEnvironment>(),
            new ConfigurationBuilder().Build());
    }

    private static QuizSessionService CreateQuizSessionService(AppDbContext dbContext)
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients
            .Setup(clients => clients.Group(It.IsAny<string>()))
            .Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<QuizHub>>();
        hubContext
            .Setup(context => context.Clients)
            .Returns(hubClients.Object);

        var hostedService = new MezonBotHostedService(
            Mock.Of<IServiceScopeFactory>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<MezonBotHostedService>>(),
            LoggerFactory.Create(_ => { }));

        return new QuizSessionService(
            dbContext,
            Mock.Of<IDynamicLinkService>(),
            hubContext.Object,
            hostedService,
            Mock.Of<ILogger<QuizSessionService>>());
    }

    private static User CreateUser(Guid id, string username)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.test"
        };
    }

    private static Quiz CreateQuiz(Guid id, Guid creatorId, List<QuizQuestion>? questions = null)
    {
        return new Quiz
        {
            Id = id,
            CreatorId = creatorId,
            Title = $"Quiz {id:N}",
            Questions = questions ?? new List<QuizQuestion>(),
            Settings = new QuizSettings(),
            Status = QuizStatus.Published,
            Visibility = QuizVisibility.Public
        };
    }

    private static QuizQuestion CreateQuestion()
    {
        return new QuizQuestion
        {
            Id = 1,
            Index = 0,
            Content = "Question?",
            Points = 10,
            QuestionType = QuestionType.SingleChoice,
            Options =
            [
                new QuizOption { Id = 1, Index = 0, Content = "A", IsCorrect = true },
                new QuizOption { Id = 2, Index = 1, Content = "B", IsCorrect = false }
            ]
        };
    }

    private sealed class DbHarness : IAsyncDisposable
    {
        private DbHarness(SqliteConnection connection, AppDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
        }

        public SqliteConnection Connection { get; }

        public AppDbContext DbContext { get; }

        public static async Task<DbHarness> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new DbHarness(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
