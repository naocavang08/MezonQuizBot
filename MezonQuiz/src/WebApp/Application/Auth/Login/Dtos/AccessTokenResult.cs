namespace WebApp.Application.Auth.Login.Dtos
{
    public class AccessTokenResult
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
