using Microsoft.AspNetCore.Authorization;

namespace WebApp.Application.Auth.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(params string[] permissions)
    {
        if (permissions is null || permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        Policy = permissions.Length == 1
            ? PermissionPolicy.For(permissions[0])
            : PermissionPolicy.ForAny(permissions);
    }
}