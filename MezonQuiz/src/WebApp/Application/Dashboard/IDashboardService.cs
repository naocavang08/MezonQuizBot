using WebApp.Application.Dashboard.Dtos;

namespace WebApp.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int days = 7, int recentLimit = 8);
}
