namespace WebApp.Authorization;

public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string For(string permission)
    {
        return Prefix + permission;
    }
}
