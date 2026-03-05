using WebApp.Application.Dtos;

namespace WebApp.Application.Interface
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<RoleDto> GetRoleByIdAsync(Guid id);
        Task<RoleDto> CreateRoleAsync(RoleRequestDto request);
        Task DeleteRoleAsync(Guid id);
        Task<List<PermissionDto>> GetAllPermissionsAsync();
        Task<List<Guid>> GetRolePermissionIdsAsync(Guid roleId);
        Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
    }
}