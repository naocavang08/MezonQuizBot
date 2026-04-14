using Microsoft.AspNetCore.Authorization;

namespace WebApp.Application.Auth.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
        : this(new[] { permission })
    {
    }

    public PermissionRequirement(IEnumerable<string> permissions)
    {
        var normalized = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        Permissions = normalized;
    }

    public IReadOnlyList<string> Permissions { get; }
}
