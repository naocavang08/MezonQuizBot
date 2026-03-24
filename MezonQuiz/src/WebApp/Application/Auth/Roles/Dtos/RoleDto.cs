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
    }
    public class RoleDto : RoleRequestDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}