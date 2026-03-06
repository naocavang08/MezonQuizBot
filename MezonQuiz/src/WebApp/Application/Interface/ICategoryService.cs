using WebApp.Application.Dtos;

namespace WebApp.Application.Interface
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<bool> CreateCategoryAsync(SaveCategoryDto request);
        Task<bool> UpdateCategoryAsync(Guid categoryId, SaveCategoryDto request);
        Task<bool> DeleteCategoryAsync(Guid categoryId);
    }
}
