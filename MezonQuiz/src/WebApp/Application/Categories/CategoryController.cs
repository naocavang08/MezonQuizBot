using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Auth.Authorization;
using WebApp.Application.Categories.Dtos;

namespace WebApp.Application.Categories
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
        [PermissionAuthorize(PermissionNames.Categories.Admin_List, PermissionNames.Categories.Creator_List, PermissionNames.Categories.Player_List)]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        [PermissionAuthorize(PermissionNames.Categories.Admin_List, PermissionNames.Categories.Creator_List, PermissionNames.Categories.Player_List)]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Categories.Create)]
        public async Task<IActionResult> CreateCategory([FromBody] SaveCategoryDto request)
        {
            try
            {
                var result = await _categoryService.CreateCategoryAsync(request);
                return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [PermissionAuthorize(PermissionNames.Categories.Update)]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] SaveCategoryDto request)
        {
            try
            {
                var result = await _categoryService.UpdateCategoryAsync(id, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [PermissionAuthorize(PermissionNames.Categories.Delete)]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return NoContent();
        }
    }
}
