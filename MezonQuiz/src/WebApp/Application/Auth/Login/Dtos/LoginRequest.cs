using System.ComponentModel.DataAnnotations;
namespace WebApp.Application.Auth.Login.Dtos
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}