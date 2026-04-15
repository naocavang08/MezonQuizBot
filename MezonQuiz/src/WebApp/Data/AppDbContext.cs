using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using WebApp.Application.AuditLog.Dtos;
using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Domain.Entites;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Data
{
      public class AppDbContext : DbContext
      {
            private readonly IHttpContextAccessor? _httpContextAccessor;
            private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

            private static readonly ValueComparer<List<QuizQuestion>> QuestionsComparer = new(
                  (left, right) => JsonSerializer.Serialize(left ?? new List<QuizQuestion>(), JsonOptions) == JsonSerializer.Serialize(right ?? new List<QuizQuestion>(), JsonOptions),
                  value => JsonSerializer.Serialize(value ?? new List<QuizQuestion>(), JsonOptions).GetHashCode(),
                  value => JsonSerializer.Deserialize<List<QuizQuestion>>(JsonSerializer.Serialize(value ?? new List<QuizQuestion>(), JsonOptions), JsonOptions) ?? new List<QuizQuestion>()
            );

            private static readonly ValueComparer<QuizSettings> SettingsComparer = new(
                  (left, right) => JsonSerializer.Serialize(left ?? new QuizSettings(), JsonOptions) == JsonSerializer.Serialize(right ?? new QuizSettings(), JsonOptions),
                  value => JsonSerializer.Serialize(value ?? new QuizSettings(), JsonOptions).GetHashCode(),
                  value => JsonSerializer.Deserialize<QuizSettings>(JsonSerializer.Serialize(value ?? new QuizSettings(), JsonOptions), JsonOptions) ?? new QuizSettings()
            );

            private static readonly ValueComparer<AuditDetailsDto> AuditDetailsComparer = new(
                  (left, right) => JsonSerializer.Serialize(left ?? new AuditDetailsDto(), JsonOptions) == JsonSerializer.Serialize(right ?? new AuditDetailsDto(), JsonOptions),
                  value => JsonSerializer.Serialize(value ?? new AuditDetailsDto(), JsonOptions).GetHashCode(),
                  value => JsonSerializer.Deserialize<AuditDetailsDto>(JsonSerializer.Serialize(value ?? new AuditDetailsDto(), JsonOptions), JsonOptions) ?? new AuditDetailsDto()
            );

            public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
            {
                  _httpContextAccessor = httpContextAccessor;
            }
            public DbSet<User> Users => Set<User>();
            public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
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

            public override int SaveChanges()
            {
                  AddAuditLogs();
                  return base.SaveChanges();
            }

            public override int SaveChanges(bool acceptAllChangesOnSuccess)
            {
                  AddAuditLogs();
                  return base.SaveChanges(acceptAllChangesOnSuccess);
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                  AddAuditLogs();
                  return base.SaveChangesAsync(cancellationToken);
            }

            public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            {
                  AddAuditLogs();
                  return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }

            private void AddAuditLogs()
            {
                  var httpContext = _httpContextAccessor?.HttpContext;
                  if (httpContext is null)
                  {
                        return;
                  }

                  var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                  var hasUserId = Guid.TryParse(userIdClaim, out var userId);
                  var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                  var now = DateTime.UtcNow;

                  var entries = ChangeTracker
                        .Entries()
                        .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                        .Where(entry => entry.Entity is not AuditLog)
                        .ToList();

                  if (entries.Count == 0)
                  {
                        return;
                  }

                  var logs = new List<AuditLog>(entries.Count);
                  foreach (var entry in entries)
                  {
                        var details = BuildAuditDetails(entry);

                        logs.Add(new AuditLog
                        {
                              Id = Guid.NewGuid(),
                              UserId = hasUserId ? userId : null,
                              Action = ResolveAction(entry.State),
                              ResourceType = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
                              ResourceId = ResolveResourceId(entry),
                              Details = details,
                              IpAddress = ipAddress,
                              CreatedAt = now,
                        });
                  }

                  AuditLogs.AddRange(logs);
            }

            private static string ResolveAction(EntityState state)
            {
                  return state switch
                  {
                        EntityState.Added => "create",
                        EntityState.Modified => "update",
                        EntityState.Deleted => "delete",
                        _ => "unknown",
                  };
            }

            private static Guid? ResolveResourceId(EntityEntry entry)
            {
                  var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                  if (keyProperty is null)
                  {
                        return null;
                  }

                  var value = entry.State == EntityState.Deleted ? keyProperty.OriginalValue : keyProperty.CurrentValue;
                  if (value is Guid guidValue)
                  {
                        return guidValue;
                  }

                  return Guid.TryParse(value?.ToString(), out var parsedGuid) ? parsedGuid : null;
            }

            private static AuditDetailsDto BuildAuditDetails(EntityEntry entry)
            {
                  var payload = new Dictionary<string, object?>
                  {
                        ["entity"] = entry.Metadata.ClrType.Name,
                  };

                  if (entry.State == EntityState.Added)
                  {
                        payload["newValues"] = ReadPropertyValues(entry.Properties, useOriginalValues: false);
                  }
                  else if (entry.State == EntityState.Deleted)
                  {
                        payload["oldValues"] = ReadPropertyValues(entry.Properties, useOriginalValues: true);
                  }
                  else if (entry.State == EntityState.Modified)
                  {
                        var changes = entry.Properties
                              .Where(property => property.IsModified)
                              .ToDictionary(
                                    property => property.Metadata.Name,
                                    property => new
                                    {
                                          oldValue = NormalizeValue(property.OriginalValue),
                                          newValue = NormalizeValue(property.CurrentValue),
                                    });

                        payload["changes"] = changes;
                  }

                  return new AuditDetailsDto
                  {
                        Title = entry.Metadata.ClrType.Name,
                        Description = JsonSerializer.Serialize(payload, JsonOptions),
                        Status = ResolveAction(entry.State),
                  };
            }

            private static Dictionary<string, object?> ReadPropertyValues(IEnumerable<PropertyEntry> properties, bool useOriginalValues)
            {
                  return properties.ToDictionary(
                        property => property.Metadata.Name,
                        property => useOriginalValues
                              ? NormalizeValue(property.OriginalValue)
                              : NormalizeValue(property.CurrentValue));
            }

            private static object? NormalizeValue(object? value)
            {
                  if (value is null)
                  {
                        return null;
                  }

                  return value switch
                  {
                        JsonDocument jsonDocument => JsonSerializer.Deserialize<object>(jsonDocument.RootElement.GetRawText(), JsonOptions),
                        JsonElement jsonElement => JsonSerializer.Deserialize<object>(jsonElement.GetRawText(), JsonOptions),
                        byte[] bytes => Convert.ToBase64String(bytes),
                        _ => value,
                  };
            }

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

                  b.Entity<RefreshToken>(entity =>
                  {
                        entity.HasIndex(e => e.TokenHash).IsUnique();
                        entity.HasIndex(e => new { e.UserId, e.ExpiresAt });

                        entity.HasOne(e => e.User)
                              .WithMany()
                              .HasForeignKey(e => e.UserId)
                              .OnDelete(DeleteBehavior.Cascade);
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
                        entity.Property(e => e.Questions)
                          .HasColumnType("jsonb")
                          .HasConversion(
                                value => JsonSerializer.Serialize(value ?? new List<QuizQuestion>(), JsonOptions),
                                value => JsonSerializer.Deserialize<List<QuizQuestion>>(value, JsonOptions) ?? new List<QuizQuestion>())
                          .Metadata.SetValueComparer(QuestionsComparer);

                        entity.Property(e => e.Settings)
                          .HasColumnType("jsonb")
                          .HasConversion(
                                value => JsonSerializer.Serialize(value ?? new QuizSettings(), JsonOptions),
                                value => JsonSerializer.Deserialize<QuizSettings>(value, JsonOptions) ?? new QuizSettings())
                          .Metadata.SetValueComparer(SettingsComparer);

                        entity.Property(e => e.Visibility)
                        .HasColumnType("quiz_visibility");

                        entity.Property(e => e.Status)
                        .HasColumnType("quiz_status");

                        entity.HasOne(e => e.Creator)
                        .WithMany()
                        .HasForeignKey(e => e.CreatorId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(e => e.Category)
                        .WithMany()
                        .HasForeignKey(e => e.CategoryId)
                        .OnDelete(DeleteBehavior.SetNull);
                  });

                  b.Entity<AuditLog>(entity =>
                  {
                        entity.Property(e => e.Details)
                        .HasColumnType("jsonb")
                        .HasConversion(
                              value => JsonSerializer.Serialize(value ?? new AuditDetailsDto(), JsonOptions),
                              value => JsonSerializer.Deserialize<AuditDetailsDto>(value, JsonOptions) ?? new AuditDetailsDto())
                        .Metadata.SetValueComparer(AuditDetailsComparer);
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
