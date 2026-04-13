using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Auth.Authorization;

namespace WebApp.Application.Dashboard;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [PermissionAuthorize(PermissionNames.Quizzes.List)]
    public async Task<IActionResult> GetSummary([FromQuery] int days = 7, [FromQuery] int recentLimit = 8)
    {
        var summary = await _dashboardService.GetSummaryAsync(days, recentLimit);
        return Ok(summary);
    }
}
