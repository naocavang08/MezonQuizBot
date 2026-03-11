using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Authorization;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [PermissionAuthorize(PermissionNames.Users.List)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [PermissionAuthorize(PermissionNames.Users.View)]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Users.Create)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
        {
            try
            {
                var createdUser = await _userService.CreateUserAsync(request);
                return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}")]
        [PermissionAuthorize(PermissionNames.Users.Update)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request)
        {
            try
            {
                var updatedUser = await _userService.UpdateUserAsync(id, request);
                return Ok(updatedUser);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [PermissionAuthorize(PermissionNames.Users.Delete)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpGet("{id}/roles")]
        [PermissionAuthorize(PermissionNames.Users.View)]
        public async Task<IActionResult> GetUserRoleIds(Guid id)
        {
            try
            {
                var roleIds = await _userService.GetUserRoleIdsAsync(id);
                return Ok(roleIds);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/roles")]
        [PermissionAuthorize(PermissionNames.Users.AssignRole)]
        public async Task<IActionResult> AssignRolesToUser(Guid id, [FromBody] List<Guid> roleIds)
        {
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (!Guid.TryParse(userIdClaimValue, out var userIdClaim))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                await _userService.AssignRolesToUserAsync(userIdClaim, id, roleIds);
                return Ok(new { Message = "Roles assigned successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }
}
