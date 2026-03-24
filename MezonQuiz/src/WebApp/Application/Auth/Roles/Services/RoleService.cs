using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Domain.Entites;
using WebApp.Application.Auth.Roles.Dtos;
using WebApp.Application.Auth.Roles;

namespace WebApp.Application.Auth.Roles.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _dbContext;

        public RoleService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _dbContext.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();

            return roles.Select(MapRoleToDto).ToList();
        }

        public async Task<RoleDto> GetRoleByIdAsync(Guid id)
        {
            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role is null)
            {
                throw new KeyNotFoundException("Role not found.");
            }

            return MapRoleToDto(role);
        }

        public async Task<RoleDto> CreateRoleAsync(RoleRequestDto request)
        {
            var normalizedName = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new ArgumentException("Role name is required.", nameof(request.Name));
            }

            var existed = await _dbContext.Roles
                .AnyAsync(r => r.Name.ToLower() == normalizedName.ToLower());

            if (existed)
            {
                throw new InvalidOperationException("Role name already exists.");
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                DisplayName = request.DisplayName?.Trim(),
                Description = request.Description?.Trim(),
                IsSystem = request.IsSystem,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();

            return MapRoleToDto(role);
        }

        public async Task DeleteRoleAsync(Guid id)
        {
            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.Id == id);

            var user = await _dbContext.UserRoles
                .Include(ur => ur.User)
                .FirstOrDefaultAsync(ur => ur.RoleId == id);

            if (user != null)
            {
                throw new InvalidOperationException("Role cannot be deleted because it is assigned to one or more users.");
            }

            if (role is null)
            {
                throw new KeyNotFoundException("Role not found.");
            }

            if (role.IsSystem)
            {
                throw new InvalidOperationException("System role cannot be deleted.");
            }

            _dbContext.Roles.Remove(role);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<PermissionDto>> GetAllPermissionsAsync()
        {
            return await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Resource = p.Resource,
                    Action = p.Action,
                    Description = p.Description
                })
                .ToListAsync();
        }

        public async Task<List<Guid>> GetRolePermissionIdsAsync(Guid roleId)
        {
            var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                throw new KeyNotFoundException("Role not found.");
            }

            return await _dbContext.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
        }

        public async Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
        {
            var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                throw new KeyNotFoundException("Role not found.");
            }

            var targetPermissionIds = (permissionIds ?? new List<Guid>())
                .Distinct()
                .ToHashSet();

            var existingPermissionIds = await _dbContext.Permissions
                .Where(p => targetPermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (existingPermissionIds.Count != targetPermissionIds.Count)
            {
                throw new ArgumentException("Some permissions do not exist.", nameof(permissionIds));
            }

            var currentRolePermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            var currentPermissionIdSet = currentRolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            var toRemove = currentRolePermissions
                .Where(rp => !targetPermissionIds.Contains(rp.PermissionId))
                .ToList();

            var toAdd = targetPermissionIds
                .Where(permissionId => !currentPermissionIdSet.Contains(permissionId))
                .Select(permissionId => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (toRemove.Count > 0)
            {
                _dbContext.RolePermissions.RemoveRange(toRemove);
            }

            if (toAdd.Count > 0)
            {
                _dbContext.RolePermissions.AddRange(toAdd);
            }

            if (toRemove.Count > 0 || toAdd.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        private static RoleDto MapRoleToDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsSystem = role.IsSystem
            };
        }
    }
}