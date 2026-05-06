using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.Auth.Roles.Dtos
{
    public class RoleRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public bool IsSystem { get; set; } = false;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentException("Role name is required.", nameof(Name));
            }

            if (Name.Trim().Length > 50)
            {
                throw new ArgumentException("Role name must not exceed 50 characters.", nameof(Name));
            }

            if (!string.IsNullOrWhiteSpace(DisplayName) && DisplayName.Trim().Length > 100)
            {
                throw new ArgumentException("Display name must not exceed 100 characters.", nameof(DisplayName));
            }
        }
    }
    public class RoleDto : RoleRequestDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
