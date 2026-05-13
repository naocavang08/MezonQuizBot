using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.Auth.Login.Dtos
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
