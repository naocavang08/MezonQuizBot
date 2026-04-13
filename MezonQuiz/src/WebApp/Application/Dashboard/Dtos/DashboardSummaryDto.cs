namespace WebApp.Application.Dashboard.Dtos;
using WebApp.Domain.Entites;

public sealed class DashboardSummaryDto
{
    public DashboardKpiDto Kpis { get; set; } = new();
    public List<DashboardStatusCountDto> QuizStatusDistribution { get; set; } = new();
    public List<DashboardStatusCountDto> SessionStatusDistribution { get; set; } = new();
    public List<DashboardCategoryStatDto> TopCategories { get; set; } = new();
    public List<DashboardDailyStatDto> DailyStats { get; set; } = new();
    public List<AuditLog> RecentActivities { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DashboardKpiDto
{
    public int Users { get; set; }
    public int Quizzes { get; set; }
    public int Categories { get; set; }
    public int Sessions { get; set; }
    public int Participants { get; set; }
    public int Answers { get; set; }
}

public sealed class DashboardStatusCountDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class DashboardCategoryStatDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int QuizCount { get; set; }
}

public sealed class DashboardDailyStatDto
{
    public DateTime Date { get; set; }
    public int Users { get; set; }
    public int Quizzes { get; set; }
    public int Sessions { get; set; }
}

