using Microsoft.EntityFrameworkCore;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Data;
using WebApp.Domain.Entites;

namespace WebApp.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _dbContext;
        public CategoryService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<bool> CreateCategoryAsync(SaveCategoryDto request)
        {
            var category = new QuizCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                Icon = request.Icon,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.QuizCategories.Add(category);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteCategoryAsync(Guid categoryId)
        {
            var category = await _dbContext.QuizCategories.FindAsync(categoryId);
            if (category == null)
                return false;
            _dbContext.QuizCategories.Remove(category);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _dbContext.QuizCategories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Icon = c.Icon,
                    SortOrder = c.SortOrder,
                    CreatedAt = c.CreatedAt,
                })
                .ToListAsync();
            return categories;
        }

        public async Task<bool> UpdateCategoryAsync(Guid categoryId, SaveCategoryDto request)
        {
            var category = await _dbContext.QuizCategories.FindAsync(categoryId);
            if (category == null) return false;
            category.Name = request.Name;
            category.SortOrder = request.SortOrder;
            category.Slug = request.Slug;
            category.Icon = request.Icon;
            _dbContext.QuizCategories.Update(category);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }
    }
}
