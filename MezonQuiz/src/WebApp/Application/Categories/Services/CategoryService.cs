using Microsoft.EntityFrameworkCore;
using WebApp.Application.Categories;
using WebApp.Application.Categories.Dtos;
using WebApp.Data;
using WebApp.Domain.Entites;

namespace WebApp.Application.Categories.Services
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
            ArgumentNullException.ThrowIfNull(request);
            request.Validate();

            var category = new QuizCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Slug = request.Slug?.Trim(),
                Icon = request.Icon?.Trim(),
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

        public async Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId)
        {
            var category = await _dbContext.QuizCategories.FindAsync(categoryId);
            if (category == null) return null;
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Icon = category.Icon,
                SortOrder = category.SortOrder,
                CreatedAt = category.CreatedAt,
            };
        }

        public async Task<bool> UpdateCategoryAsync(Guid categoryId, SaveCategoryDto request)
        {
            ArgumentNullException.ThrowIfNull(request);
            request.Validate();

            var category = await _dbContext.QuizCategories.FindAsync(categoryId);
            if (category == null) return false;
            category.Name = request.Name.Trim();
            category.SortOrder = request.SortOrder;
            category.Slug = request.Slug?.Trim();
            category.Icon = request.Icon?.Trim();
            _dbContext.QuizCategories.Update(category);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }
    }
}
