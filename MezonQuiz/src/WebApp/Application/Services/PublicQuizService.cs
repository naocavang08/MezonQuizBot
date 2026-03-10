using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Data;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.Services
{
    public class PublicQuizService : IPublicQuizService
    {
        private readonly AppDbContext _context;
        public PublicQuizService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<PagedQuizListDto<PublicQuizDto>> GetAllPublicQuizzesAsync(QuizListQuery query)
        {
            query ??= new QuizListQuery();

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);

            var quizzesQuery = _context.Quizzes
                .AsNoTracking()
                .Where(q => q.Status == QuizStatus.Published)
                .Where(q =>
                    q.Visibility == QuizVisibility.Public ||
                    (q.Visibility == QuizVisibility.Private
                        && query.UserId.HasValue
                        && q.CreatorId == query.UserId))
                .Where(q => !query.Category.HasValue || q.CategoryId == query.Category.Value)
                .Where(q => string.IsNullOrEmpty(query.Title) || q.Title.Contains(query.Title));


            var totalCount = await quizzesQuery.CountAsync();
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedQuizzes = await quizzesQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(q => new PublicQuizDto
                {
                    Id = q.Id,
                    Title = q.Title
                })
                .ToListAsync();

            return new PagedQuizListDto<PublicQuizDto>
            {
                Items = pagedQuizzes,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
    }
}
