using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] SaveCategoryDto request)
        {
            var result = await _categoryService.CreateCategoryAsync(request);
            if (result) return Ok();
            return BadRequest("Failed to create category");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] SaveCategoryDto request)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, request);
            if (result) return Ok();
            return NotFound("Category not found");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (result) return Ok();
            return NotFound("Category not found");
        }
    }
}
