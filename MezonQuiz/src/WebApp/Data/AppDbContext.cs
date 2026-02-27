using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using WebApp.Domain.Entites;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
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

            b.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.MezonUserId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            b.Entity<Role>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });

            b.Entity<Permission>(entity =>
            {
                entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();
            });

            b.Entity<UserRole>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

                // user_id ON DELETE CASCADE
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // role_id ON DELETE CASCADE
                entity.HasOne(e => e.Role)
                      .WithMany()
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                // assigned_by ON DELETE SET NULL
                entity.HasOne(e => e.AssignedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.AssignedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<RolePermission>(entity =>
            {
                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();

                entity.HasOne(e => e.Role)
                      .WithMany()
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                      .WithMany()
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<QuizCategory>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();
            });

            b.Entity<Quiz>(entity =>
            {
                entity.Property(e => e.Visibility)
                      .HasColumnType("quiz_visibility");

                entity.Property(e => e.Status)
                      .HasColumnType("quiz_status");

                entity.OwnsOne(e => e.Settings, settings =>
                {
                    settings.ToJson("settings");
                });

                entity.OwnsMany(e => e.Questions, questions =>
                {
                    questions.ToJson("questions");
                    questions.OwnsMany(q => q.Options);
                });

                entity.HasOne(e => e.Creator)
                      .WithMany()
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                      .WithMany()
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<QuizSession>(entity =>
            {

                entity.Property(e => e.Status)
                      .HasColumnType("session_status");

                entity.HasOne(e => e.Quiz)
                      .WithMany()
                      .HasForeignKey(e => e.QuizId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Host)
                      .WithMany()
                      .HasForeignKey(e => e.HostId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<SessionParticipant>(entity =>
            {
                entity.HasIndex(e => new { e.SessionId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Session)
                      .WithMany()
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Answer>(entity =>
            {
                entity.HasIndex(e => new { e.SessionId, e.UserId, e.QuestionIndex }).IsUnique();

                entity.HasOne(e => e.Session)
                      .WithMany()
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
