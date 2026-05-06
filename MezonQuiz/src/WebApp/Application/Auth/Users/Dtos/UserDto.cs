using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace WebApp.Application.Auth.Users.Dtos
{
    public class CreateUserRequestDto
    {
        public string? Email { get; set; }
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                throw new ArgumentException("Username is required.", nameof(Username));
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                throw new ArgumentException("Password is required.", nameof(Password));
            }

            if (string.IsNullOrWhiteSpace(Email) || !UserRequestValidation.IsValidEmail(Email))
            {
                throw new ArgumentException("Valid email is required.", nameof(Email));
            }
        }
    }

    public class UpdateUserRequestDto
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Email) || !UserRequestValidation.IsValidEmail(Email))
            {
                throw new ArgumentException("Valid email is required.", nameof(Email));
            }
        }
    }

    public class UserDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? MezonUserId { get; set; }

        public string? Email { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        public string? DisplayName { get; set; }

        public string? AvatarUrl { get; set; }

        public bool HasPassword { get; set; }

        public bool IsOAuthUser { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class UploadAvatarRequestDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    internal static class UserRequestValidation
    {
        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
