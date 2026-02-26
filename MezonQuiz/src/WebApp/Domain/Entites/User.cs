namespace WebApp.Domain.Entites
{
    public class User
    {
        public Guid Id { get; set; }
        public string MezonUserId { get; set; } = null!;
        public string? Email { get; set; }
        public string Username { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
