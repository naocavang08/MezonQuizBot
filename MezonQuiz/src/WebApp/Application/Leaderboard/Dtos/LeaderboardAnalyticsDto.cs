namespace WebApp.Application.Leaderboard.Dtos;

public sealed class TopUserAnalyticsQueryDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }
    public int? MinSessions { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class TopUserAnalyticsResponseDto
{
    public TopUserAnalyticsSummaryDto Summary { get; set; } = new();
    public List<TopUserAnalyticsRowDto> Items { get; set; } = new();
    public TopUserAnalyticsPaginationDto Pagination { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TopUserAnalyticsSummaryDto
{
    public int TotalActiveUsers { get; set; }
    public int TotalParticipations { get; set; }
    public int TotalSessions { get; set; }
    public double AverageAccuracy { get; set; }
    public double AverageScorePerUser { get; set; }
}

public sealed class TopUserAnalyticsRowDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TotalScore { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
    public double AccuracyRate { get; set; }
    public int TotalSessions { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

public sealed class TopUserAnalyticsPaginationDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
