using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entites;

namespace WebApp.Data
{
    public static class Seeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var permissionSeeds = new (string Resource, string Action, string Description)[]
            {
                ("users", "list", "View user list"),
                ("users", "view", "View user detail"),
                ("users", "create", "Create new user"),
                ("users", "update", "Edit user info"),
                ("users", "delete", "Delete user"),
                ("users", "assign_role", "Assign roles to users"),

                ("roles", "list", "View role list"),
                ("roles", "view", "View role detail"),
                ("roles", "create", "Create new role"),
                ("roles", "update", "Edit role"),
                ("roles", "delete", "Delete role"),
                ("roles", "assign_permission", "Assign permissions to roles"),

                ("quizzes", "admin_list", "View quiz list"),
                ("quizzes", "creator_list", "View quizzes created by the user"),
                ("quizzes", "player_list", "View quizzes available to play"),
                ("quizzes", "admin_view", "View quiz detail"),
                ("quizzes", "creator_view", "View quiz detail"),
                ("quizzes", "player_view", "View quiz detail"),
                ("quizzes", "create", "Create quiz"),
                ("quizzes", "update", "Edit quiz"),
                ("quizzes", "delete", "Delete quiz"),
                ("quizzes", "publish", "Publish quiz"),
                ("quizzes", "moderate", "Moderate quiz content"),

                ("categories", "admin_list", "View category list"),
                ("categories", "creator_list", "View categories created by the user"),
                ("categories", "player_list", "View categories available to play"),
                ("categories", "create", "Create category"),
                ("categories", "update", "Update category"),
                ("categories", "delete", "Delete category"),

                ("sessions", "admin_list", "View session list"),
                ("sessions", "creator_list", "View sessions created by the user"),
                ("sessions", "player_list", "View sessions available to play"),
                ("sessions", "admin_view", "View session detail"),
                ("sessions", "creator_view", "View session detail"),
                ("sessions", "player_view", "View session detail"),
                ("sessions", "create", "Create session"),
                ("sessions", "moderate", "Moderate session"),
                ("sessions", "delete", "Delete session"),

                ("reports", "view", "View reports"),
                ("reports", "export", "Export reports"),

                ("audit_logs", "list", "View audit log list"),
                ("audit_logs", "view", "View audit log detail"),

                ("settings", "view", "View system settings"),
                ("settings", "update", "Update system settings")
            };

            var seedMap = permissionSeeds
                .ToDictionary(
                    p => $"{p.Resource}:{p.Action}",
                    p => p,
                    StringComparer.OrdinalIgnoreCase);

            var existingPermissions = await context.Permissions
                .ToListAsync();

            var existingKeySet = existingPermissions
                .Select(p => $"{p.Resource}:{p.Action}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingPermissions = permissionSeeds
                .Where(p => !existingKeySet.Contains($"{p.Resource}:{p.Action}"))
                .Select(p => new Permission
                {
                    Id = Guid.NewGuid(),
                    Resource = p.Resource,
                    Action = p.Action,
                    Description = p.Description,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            foreach (var permission in existingPermissions)
            {
                var key = $"{permission.Resource}:{permission.Action}";
                if (!seedMap.TryGetValue(key, out var seed))
                {
                    continue;
                }

                if (!string.Equals(permission.Description, seed.Description, StringComparison.Ordinal))
                {
                    permission.Description = seed.Description;
                }
            }

            if (missingPermissions.Count > 0)
            {
                context.Permissions.AddRange(missingPermissions);
            }

            await context.SaveChangesAsync();

            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == "super_admin");

            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "super_admin",
                    DisplayName = "Super Admin",
                    Description = "Full system access",
                    IsSystem = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Roles.Add(adminRole);
                await context.SaveChangesAsync();
            }

            var adminUser = await context.Users
                .FirstOrDefaultAsync(u => u.Username == "superadmin");

            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "superadmin",
                    Email = "superadmin@ncc.asia",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    DisplayName = "Super Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }

            var adminUserRoleExists = await context.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);

            if (!adminUserRoleExists)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
            }

            var allPermissions = await context.Permissions
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync();

            var existingRolePermissionIds = await context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == adminRole.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var existingRolePermissionSet = existingRolePermissionIds.ToHashSet();

            var missingRolePermissions = allPermissions
                .Where(permissionId => !existingRolePermissionSet.Contains(permissionId))
                .Select(permissionId => new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permissionId
                })
                .ToList();

            if (missingRolePermissions.Count > 0)
            {
                context.RolePermissions.AddRange(missingRolePermissions);
                await context.SaveChangesAsync();
            }
        }
    }
}
