using WebApp.Application.Search.Dtos;

namespace WebApp.Application.Search;

public interface ISearchService
{
    Task<GlobalSearchResultDto> SearchGlobalAsync(string keyword, int limitPerCategory, Guid userId);
}
