namespace WebApp.Application.Auth.Login.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public object User { get; set; } = null!;
        public List<string> RoleName { get; set; } = new();
        public List<string> PermissionName { get; set; } = new();
        public bool HasSystemRole { get; set; }
    }
}
