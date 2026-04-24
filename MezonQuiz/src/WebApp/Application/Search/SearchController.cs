using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Search.Dtos;

namespace WebApp.Application.Search;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("global")]
    public async Task<ActionResult<GlobalSearchResultDto>> SearchGlobal([FromQuery] string keyword, [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Ok(new GlobalSearchResultDto());
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        // Limit results to a reasonable amount if client requests too many
        limit = Math.Clamp(limit, 1, 100);

        var result = await _searchService.SearchGlobalAsync(keyword, limit, userId);
        return Ok(result);
    }
}
