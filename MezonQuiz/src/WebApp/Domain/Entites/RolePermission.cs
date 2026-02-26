namespace WebApp.Domain.Entites
{
    public class RolePermission
    {
        public Guid Id { get; set; }

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
