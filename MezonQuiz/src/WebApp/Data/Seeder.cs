using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entites;

namespace WebApp.Data
{
    public static class Seeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!context.Users.Any(u => u.Username == "superadmin"))
            {
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

                var adminUser = new User
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

                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
