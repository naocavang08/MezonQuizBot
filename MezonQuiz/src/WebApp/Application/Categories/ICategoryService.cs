using WebApp.Application.Categories.Dtos;

namespace WebApp.Application.Categories
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId);
        Task<bool> CreateCategoryAsync(SaveCategoryDto request);
        Task<bool> UpdateCategoryAsync(Guid categoryId, SaveCategoryDto request);
        Task<bool> DeleteCategoryAsync(Guid categoryId);
    }
}
