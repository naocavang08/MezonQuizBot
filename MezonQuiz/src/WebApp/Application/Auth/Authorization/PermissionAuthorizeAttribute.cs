using Microsoft.AspNetCore.Authorization;

namespace WebApp.Application.Auth.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(string permission)
    {
        Policy = PermissionPolicy.For(permission);
    }
}