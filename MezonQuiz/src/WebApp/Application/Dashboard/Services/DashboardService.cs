using Microsoft.EntityFrameworkCore;
using WebApp.Application.AuditLog;
using WebApp.Application.Dashboard.Dtos;
using WebApp.Data;
using WebApp.Domain.Entites;

namespace WebApp.Application.Dashboard.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public DashboardService(AppDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int days = 7, int recentLimit = 8)
    {
        var safeDays = Math.Clamp(days, 1, 30);
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 30);
        var startDate = DateTime.UtcNow.Date.AddDays(-(safeDays - 1));

        var kpis = await BuildKpisAsync();
        var quizStatusDistribution = await BuildQuizStatusDistributionAsync();
        var sessionStatusDistribution = await BuildSessionStatusDistributionAsync();
        var topCategories = await BuildTopCategoriesAsync();
        var dailyStats = await BuildDailyStatsAsync(startDate, safeDays);

        return new DashboardSummaryDto
        {
            Kpis = kpis,
            QuizStatusDistribution = quizStatusDistribution,
            SessionStatusDistribution = sessionStatusDistribution,
            TopCategories = topCategories,
            DailyStats = dailyStats,
            GeneratedAt = DateTime.UtcNow,
        };
    }

    private async Task<DashboardKpiDto> BuildKpisAsync()
    {
        return new DashboardKpiDto
        {
            Users = await _dbContext.Users.AsNoTracking().CountAsync(),
            Quizzes = await _dbContext.Quizzes.AsNoTracking().CountAsync(),
            Categories = await _dbContext.QuizCategories.AsNoTracking().CountAsync(),
            Sessions = await _dbContext.QuizSessions.AsNoTracking().CountAsync(),
            Participants = await _dbContext.SessionParticipants.AsNoTracking().CountAsync(),
            Answers = await _dbContext.Answers.AsNoTracking().CountAsync(),
        };
    }

    private async Task<List<DashboardStatusCountDto>> BuildQuizStatusDistributionAsync()
    {
        return await _dbContext.Quizzes
            .AsNoTracking()
            .GroupBy(quiz => quiz.Status)
            .Select(group => new DashboardStatusCountDto
            {
                Label = group.Key.ToString(),
                Count = group.Count(),
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToListAsync();
    }

    private async Task<List<DashboardStatusCountDto>> BuildSessionStatusDistributionAsync()
    {
        return await _dbContext.QuizSessions
            .AsNoTracking()
            .GroupBy(session => session.Status)
            .Select(group => new DashboardStatusCountDto
            {
                Label = group.Key.ToString(),
                Count = group.Count(),
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToListAsync();
    }

    private async Task<List<DashboardCategoryStatDto>> BuildTopCategoriesAsync()
    {
        var categories = await _dbContext.QuizCategories
            .AsNoTracking()
            .GroupJoin(
                _dbContext.Quizzes.AsNoTracking(),
                category => category.Id,
                quiz => quiz.CategoryId,
                (category, quizzes) => new DashboardCategoryStatDto
                {
                    CategoryName = category.Name,
                    QuizCount = quizzes.Count(),
                })
            .OrderByDescending(item => item.QuizCount)
            .ThenBy(item => item.CategoryName)
            .Take(5)
            .ToListAsync();

        var uncategorizedCount = await _dbContext.Quizzes
            .AsNoTracking()
            .CountAsync(quiz => quiz.CategoryId == null);

        if (uncategorizedCount > 0)
        {
            categories.Add(new DashboardCategoryStatDto
            {
                CategoryName = "Uncategorized",
                QuizCount = uncategorizedCount,
            });
        }

        return categories
            .OrderByDescending(item => item.QuizCount)
            .ThenBy(item => item.CategoryName)
            .Take(5)
            .ToList();
    }

    private async Task<List<DashboardDailyStatDto>> BuildDailyStatsAsync(DateTime startDate, int days)
    {
        var usersByDate = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.CreatedAt >= startDate)
            .GroupBy(item => item.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Date, item => item.Count);

        var quizzesByDate = await _dbContext.Quizzes
            .AsNoTracking()
            .Where(item => item.CreatedAt >= startDate)
            .GroupBy(item => item.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Date, item => item.Count);

        var sessionsByDate = await _dbContext.QuizSessions
            .AsNoTracking()
            .Where(item => item.CreatedAt >= startDate)
            .GroupBy(item => item.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Date, item => item.Count);

        var stats = new List<DashboardDailyStatDto>(days);
        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            stats.Add(new DashboardDailyStatDto
            {
                Date = date,
                Users = usersByDate.GetValueOrDefault(date, 0),
                Quizzes = quizzesByDate.GetValueOrDefault(date, 0),
                Sessions = sessionsByDate.GetValueOrDefault(date, 0),
            });
        }

        return stats;
    }

}
