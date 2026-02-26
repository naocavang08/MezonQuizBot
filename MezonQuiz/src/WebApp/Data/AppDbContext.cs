using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entites;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<QuizCategory> QuizCategories => Set<QuizCategory>();
        public DbSet<QuizSession> QuizSessions => Set<QuizSession>();
        public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();
        public DbSet<Answer> Answers => Set<Answer>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.HasPostgresEnum<QuizVisibility>();
            b.HasPostgresEnum<QuizStatus>();
            b.HasPostgresEnum<SessionStatus>();

            b.Entity<User>(e =>
            {
                e.HasIndex(x => x.MezonUserId).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
            });

            b.Entity<Permission>()
                .HasIndex(p => new { p.Resource, p.Action })
                .IsUnique();

            b.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            b.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            b.Entity<SessionParticipant>()
                .HasIndex(sp => new { sp.SessionId, sp.UserId })
                .IsUnique();

            b.Entity<Answer>()
                .HasIndex(a => new { a.SessionId, a.UserId, a.QuestionIndex })
                .IsUnique();
        }
    }
}
