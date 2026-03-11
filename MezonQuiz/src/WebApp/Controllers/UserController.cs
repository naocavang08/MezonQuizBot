using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Authorization;
using WebApp.Data;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, AppDbContext dbContext, ILogger<UserController> logger)
        {
            _userService = userService;
            _dbContext = dbContext;
            _logger = logger;
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
            var authorizeResult = await EnsureSelfOrSystemAsync(id);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

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
            var authorizeResult = await EnsureSelfOrSystemAsync(id);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

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
            var authorizeResult = await EnsureSelfOrSystemAsync(id);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

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
            var authorizeResult = await EnsureSelfOrSystemAsync(id);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

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

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var parsed = Guid.TryParse(userIdClaimValue, out userId);
            if (!parsed)
            {
                _logger.LogWarning("Unauthorized user request: missing/invalid NameIdentifier claim.");
            }

            return parsed;
        }

        private async Task<IActionResult?> EnsureSelfOrSystemAsync(Guid targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            if (currentUserId == targetUserId)
            {
                return null;
            }

            var hasSystemRole = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == currentUserId)
                .Join(_dbContext.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => r.IsSystem)
                .AnyAsync(isSystem => isSystem);

            if (!hasSystemRole)
            {
                _logger.LogWarning(
                    "Forbidden user resource access: RequestUserId={RequestUserId}, TargetUserId={TargetUserId}.",
                    currentUserId,
                    targetUserId);
                return Forbid();
            }

            return null;
        }
    }
}
