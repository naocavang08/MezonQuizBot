using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Leaderboard.Dtos;

namespace WebApp.Application.Leaderboard;

[ApiController]
[Route("api/[controller]")]
public sealed class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTopUsers([FromQuery] TopUserAnalyticsQueryDto query)
    {
        var analytics = await _leaderboardService.GetTopUsersAsync(query);
        return Ok(analytics);
    }
}
