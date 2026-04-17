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
    }

    public class UpdateUserRequestDto
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
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
}