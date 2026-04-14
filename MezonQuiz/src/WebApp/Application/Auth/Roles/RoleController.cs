using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Auth.Authorization;
using WebApp.Application.Auth.Roles.Dtos;

namespace WebApp.Application.Auth.Roles
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        [PermissionAuthorize(PermissionNames.Roles.List)]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        [PermissionAuthorize(PermissionNames.Roles.View)]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                return Ok(role);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Role not found." });
            }
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Roles.Create)]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto request)
        {
            try
            {
                var createdRole = await _roleService.CreateRoleAsync(request);
                return CreatedAtAction(nameof(GetRoleById), new { id = createdRole.Id }, createdRole);
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
        [PermissionAuthorize(PermissionNames.Roles.Delete)]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                await _roleService.DeleteRoleAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Role not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpGet("permissions")]
        [PermissionAuthorize(PermissionNames.Roles.View)]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _roleService.GetAllPermissionsAsync();
            return Ok(permissions);
        }

        [HttpGet("{id}/permissions")]
        [PermissionAuthorize(PermissionNames.Roles.View)]
        public async Task<IActionResult> GetRolePermissions(Guid id)
        {
            try
            {
                var permissionIds = await _roleService.GetRolePermissionIdsAsync(id);
                return Ok(permissionIds);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Role not found." });
            }
        }

        [HttpPost("{id}/permissions")]
        [PermissionAuthorize(PermissionNames.Roles.AssignPermission)]
        public async Task<IActionResult> AssignPermissionsToRole(Guid id, [FromBody] List<Guid> permissionIds)
        {
            try
            {
                await _roleService.AssignPermissionsToRoleAsync(id, permissionIds);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Role not found." });
            }
        }
    }
}