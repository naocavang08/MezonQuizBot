using Microsoft.EntityFrameworkCore;
using WebApp.Application.Leaderboard.Dtos;
using WebApp.Data;
using WebApp.Domain.Entites;

namespace WebApp.Application.Leaderboard.Services;

public sealed class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _dbContext;

    public LeaderboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TopUserAnalyticsResponseDto> GetTopUsersAsync(TopUserAnalyticsQueryDto query)
    {
        var safeQuery = query ?? new TopUserAnalyticsQueryDto();
        var page = Math.Max(1, safeQuery.Page);
        var pageSize = Math.Clamp(safeQuery.PageSize, 5, 100);
        var minSessions = Math.Max(0, safeQuery.MinSessions ?? 0);
        var searchTerm = (safeQuery.Search ?? string.Empty).Trim();
        var fromDate = safeQuery.DateFrom?.Date;
        var toDateExclusive = safeQuery.DateTo?.Date.AddDays(1);
        var sortBy = (safeQuery.SortBy ?? "totalscore").Trim().ToLowerInvariant();
        var sortDirection = (safeQuery.SortDirection ?? "desc").Trim().ToLowerInvariant();
        var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var participantsQuery = _dbContext.SessionParticipants
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            participantsQuery = participantsQuery.Where(item => item.JoinedAt >= fromDate.Value);
        }

        if (toDateExclusive.HasValue)
        {
            participantsQuery = participantsQuery.Where(item => item.JoinedAt < toDateExclusive.Value);
        }

        var aggregatedQuery = participantsQuery
            .GroupBy(item => new
            {
                item.UserId,
                item.User.Username,
                item.User.DisplayName,
                item.User.AvatarUrl
            })
            .Select(group => new TopUserAnalyticsRowDto
            {
                UserId = group.Key.UserId,
                DisplayName = string.IsNullOrEmpty(group.Key.DisplayName)
                    ? (group.Key.Username ?? group.Key.UserId.ToString())
                    : group.Key.DisplayName!,
                AvatarUrl = group.Key.AvatarUrl,
                TotalScore = group.Sum(item => item.TotalScore),
                TotalCorrectAnswers = group.Sum(item => item.CorrectCount),
                TotalAnswers = group.Sum(item => item.AnswersCount),
                AccuracyRate = group.Sum(item => item.AnswersCount) == 0
                    ? 0
                    : (double)group.Sum(item => item.CorrectCount) * 100 / group.Sum(item => item.AnswersCount),
                TotalSessions = group.Select(item => item.SessionId).Distinct().Count(),
                FirstSeenAt = group.Min(item => item.JoinedAt),
                LastSeenAt = group.Max(item => item.JoinedAt),
            });

        if (minSessions > 0)
        {
            aggregatedQuery = aggregatedQuery.Where(item => item.TotalSessions >= minSessions);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowered = searchTerm.ToLower();
            aggregatedQuery = aggregatedQuery.Where(item => item.DisplayName.ToLower().Contains(lowered));
        }

        var summary = await BuildTopUserSummaryAsync(aggregatedQuery, participantsQuery);

        var orderedQuery = ApplyTopUserSorting(aggregatedQuery, sortBy, isAscending);
        var totalCount = await orderedQuery.CountAsync();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var skip = (page - 1) * pageSize;
        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        for (var index = 0; index < items.Count; index++)
        {
            items[index].Rank = skip + index + 1;
        }

        return new TopUserAnalyticsResponseDto
        {
            Summary = summary,
            Items = items,
            Pagination = new TopUserAnalyticsPaginationDto
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static IQueryable<TopUserAnalyticsRowDto> ApplyTopUserSorting(
        IQueryable<TopUserAnalyticsRowDto> query,
        string sortBy,
        bool isAscending)
    {
        return (sortBy, isAscending) switch
        {
            ("displayname", true) => query
                .OrderBy(item => item.DisplayName)
                .ThenByDescending(item => item.TotalScore)
                .ThenByDescending(item => item.AccuracyRate),
            ("displayname", false) => query
                .OrderByDescending(item => item.DisplayName)
                .ThenByDescending(item => item.TotalScore)
                .ThenByDescending(item => item.AccuracyRate),
            ("accuracy", true) => query
                .OrderBy(item => item.AccuracyRate)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("accuracy", false) => query
                .OrderByDescending(item => item.AccuracyRate)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalsessions", true) => query
                .OrderBy(item => item.TotalSessions)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalsessions", false) => query
                .OrderByDescending(item => item.TotalSessions)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("lastseenat", true) => query
                .OrderBy(item => item.LastSeenAt)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("lastseenat", false) => query
                .OrderByDescending(item => item.LastSeenAt)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalcorrectanswers", true) => query
                .OrderBy(item => item.TotalCorrectAnswers)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalcorrectanswers", false) => query
                .OrderByDescending(item => item.TotalCorrectAnswers)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalanswers", true) => query
                .OrderBy(item => item.TotalAnswers)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalanswers", false) => query
                .OrderByDescending(item => item.TotalAnswers)
                .ThenByDescending(item => item.TotalScore)
                .ThenBy(item => item.DisplayName),
            ("totalscore", true) => query
                .OrderBy(item => item.TotalScore)
                .ThenByDescending(item => item.AccuracyRate)
                .ThenBy(item => item.DisplayName),
            _ => query
                .OrderByDescending(item => item.TotalScore)
                .ThenByDescending(item => item.AccuracyRate)
                .ThenBy(item => item.DisplayName)
        };
    }

    private static async Task<TopUserAnalyticsSummaryDto> BuildTopUserSummaryAsync(
        IQueryable<TopUserAnalyticsRowDto> aggregatedQuery,
        IQueryable<SessionParticipant> participantsQuery)
    {
        var totalActiveUsers = await aggregatedQuery.CountAsync();
        var totalParticipations = await participantsQuery.CountAsync();
        var totalSessions = await participantsQuery
            .Select(item => item.SessionId)
            .Distinct()
            .CountAsync();
        var totalAnswers = await aggregatedQuery.SumAsync(item => item.TotalAnswers);
        var totalCorrect = await aggregatedQuery.SumAsync(item => item.TotalCorrectAnswers);
        var averageScorePerUser = totalActiveUsers == 0
            ? 0
            : await aggregatedQuery.AverageAsync(item => (double)item.TotalScore);

        var accuracy = totalAnswers == 0 ? 0 : (double)totalCorrect * 100 / totalAnswers;

        return new TopUserAnalyticsSummaryDto
        {
            TotalActiveUsers = totalActiveUsers,
            TotalParticipations = totalParticipations,
            TotalSessions = totalSessions,
            AverageAccuracy = Math.Round(accuracy, 2),
            AverageScorePerUser = Math.Round(averageScorePerUser, 2),
        };
    }
}
