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
        public async Task<CategoryDto> CreateCategoryAsync(SaveCategoryDto request)
        {
            ArgumentNullException.ThrowIfNull(request);
            request.Validate();

            var existCategory = await _dbContext.QuizCategories
                .FirstOrDefaultAsync(c =>
                    c.Name.ToLower() == request.Name.ToLower() ||
                    (c.Slug.ToLower() == request.Slug.ToLower()));

            if (existCategory != null)
            {
                throw new ArgumentException("Category name or slug already exists.");
            }

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
            await _dbContext.SaveChangesAsync();
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

        public async Task DeleteCategoryAsync(Guid categoryId)
        {
            var category = await _dbContext.QuizCategories.FindAsync(categoryId);
            if (category != null)
            {
                _dbContext.QuizCategories.Remove(category);
                await _dbContext.SaveChangesAsync();
            }
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

        public async Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, SaveCategoryDto request)
        {
            ArgumentNullException.ThrowIfNull(request);
            request.Validate();

            var category = await _dbContext.QuizCategories.FindAsync(categoryId);
            if (category == null) throw new ArgumentException("Category not found.");
            category.Name = request.Name.Trim();
            category.SortOrder = request.SortOrder;
            category.Slug = request.Slug?.Trim();
            category.Icon = request.Icon?.Trim();
            _dbContext.QuizCategories.Update(category);
            await _dbContext.SaveChangesAsync();
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
    }
}
