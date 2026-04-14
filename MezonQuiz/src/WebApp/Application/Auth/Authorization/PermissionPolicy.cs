namespace WebApp.Application.Auth.Authorization;

public static class PermissionPolicy
{
    public const string Prefix = "perm:";
    public const string AnyPrefix = "perm-any:";

    public static string For(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be empty.", nameof(permission));
        }

        return Prefix + permission;
    }

    public static string ForAny(params string[] permissions)
    {
        if (permissions is null || permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        var normalized = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one non-empty permission is required.", nameof(permissions));
        }

        if (normalized.Length == 1)
        {
            return For(normalized[0]);
        }

        return AnyPrefix + string.Join(',', normalized);
    }
}
