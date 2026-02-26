namespace WebApp.Domain.Entites
{
    public class UserRole
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid? AssignedBy { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
