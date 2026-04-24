using Microsoft.EntityFrameworkCore;
using WebApp.Application.Search.Dtos;
using WebApp.Data;
using WebApp.Domain.Enums;

namespace WebApp.Application.Search.Services;

public class SearchService : ISearchService
{
    private readonly AppDbContext _context;

    public SearchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(string keyword, int limitPerCategory, Guid userId)
    {
        var result = new GlobalSearchResultDto();
        var loweredKeyword = keyword.ToLower();

        var quizzes = await _context.Quizzes
            .AsNoTracking()
            .Where(q => q.Title.ToLower().Contains(loweredKeyword) &&
                        (q.Visibility == Status.QuizVisibility.Public || q.CreatorId == userId))
            .OrderByDescending(q => q.CreatedAt)
            .Take(limitPerCategory)
            .Select(q => new SearchItemDto
            {
                Id = q.Id.ToString(),
                Title = q.Title,
                Type = "Quiz",
                Url = $"/app/find-quizzes/{q.Id}",
                Description = q.Description
            })
            .ToListAsync();

        result.Quizzes = quizzes;

        var categories = await _context.QuizCategories
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(loweredKeyword))
            .OrderBy(c => c.SortOrder)
            .Take(limitPerCategory)
            .Select(c => new SearchItemDto
            {
                Id = c.Id.ToString(),
                Title = c.Name,
                Type = "Category",
                Url = $"/app/find-quizzes?category={c.Id}", 
                Description = "Quiz Category"
            })
            .ToListAsync();

        result.Categories = categories;

        return result;
    }
}
