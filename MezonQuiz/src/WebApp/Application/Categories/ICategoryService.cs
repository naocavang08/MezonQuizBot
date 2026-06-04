using WebApp.Application.Categories.Dtos;

namespace WebApp.Application.Categories
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId);
        Task<CategoryDto> CreateCategoryAsync(SaveCategoryDto request);
        Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, SaveCategoryDto request);
        Task DeleteCategoryAsync(Guid categoryId);
    }
}
