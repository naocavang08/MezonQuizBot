namespace WebApp.Application.Auth.Roles.Dtos
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Resource { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? Description { get; set; }
    }
}