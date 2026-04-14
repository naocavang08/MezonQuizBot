using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Application.Auth.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AppDbContext _dbContext;

    public PermissionAuthorizationHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var requiredPermissions = requirement.Permissions
            .Select(p => p.ToLowerInvariant())
            .ToArray();

        var hasPermission = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_dbContext.RolePermissions.AsNoTracking(), ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(_dbContext.Permissions.AsNoTracking(), permissionId => permissionId, p => p.Id, (permissionId, p) => new { p.Resource, p.Action })
            .AnyAsync(p => requiredPermissions.Contains((p.Resource + "." + p.Action).ToLower()));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
