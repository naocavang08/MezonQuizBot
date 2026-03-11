using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Authorization;

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
        [PermissionAuthorize(PermissionNames.Categories.List)]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Categories.Create)]
        public async Task<IActionResult> CreateCategory([FromBody] SaveCategoryDto request)
        {
            var result = await _categoryService.CreateCategoryAsync(request);
            if (result) return Ok();
            return BadRequest("Failed to create category");
        }

        [HttpPut("{id}")]
        [PermissionAuthorize(PermissionNames.Categories.Update)]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] SaveCategoryDto request)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, request);
            if (result) return Ok();
            return NotFound("Category not found");
        }

        [HttpDelete("{id}")]
        [PermissionAuthorize(PermissionNames.Categories.Delete)]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (result) return Ok();
            return NotFound("Category not found");
        }
    }
}
