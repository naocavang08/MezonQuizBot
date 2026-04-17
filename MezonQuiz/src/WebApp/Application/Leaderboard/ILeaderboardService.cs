using WebApp.Application.Leaderboard.Dtos;

namespace WebApp.Application.Leaderboard;

public interface ILeaderboardService
{
    Task<TopUserAnalyticsResponseDto> GetTopUsersAsync(TopUserAnalyticsQueryDto query);
}
