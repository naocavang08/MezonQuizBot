using Microsoft.EntityFrameworkCore;
using WebApp.Application.Auth.Users;
using WebApp.Application.Auth.Users.Dtos;
using WebApp.Data;
using WebApp.Domain.Entites;

namespace WebApp.Application.Auth.Users.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext dbContext, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public async Task AssignRolesToUserAsync(Guid assignedId, Guid userId, List<Guid> roleIds)
        {
            var userExist = await _dbContext.Users.AnyAsync(r => r.Id == userId);
            if (!userExist)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var assignedByUserExist = await _dbContext.Users.AnyAsync(r => r.Id == assignedId);
            if (!assignedByUserExist)
            {
                throw new KeyNotFoundException("Assigned by user not found.");
            }

            var targetRoleIds = (roleIds ?? new List<Guid>())
                .Distinct()
                .ToHashSet();

            var existingRoleIds = await _dbContext.Roles
                .Where(p => targetRoleIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (existingRoleIds.Count != targetRoleIds.Count)
            {
                throw new ArgumentException("Some roles do not exist.", nameof(roleIds));
            }

            var currentUserRoles = await _dbContext.UserRoles
                .Where(rp => rp.UserId == userId)
                .ToListAsync();

            var currentRoleIdSet = currentUserRoles
                .Select(rp => rp.RoleId)
                .ToHashSet();

            var toRemove = currentUserRoles
                .Where(rp => !targetRoleIds.Contains(rp.RoleId))
                .ToList();

            var assignedByUser = await _dbContext.Users
                .FirstOrDefaultAsync(r => r.Id == assignedId);

            var toAdd = targetRoleIds
                .Where(roleId => !currentRoleIdSet.Contains(roleId))
                .Select(roleId => new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    AssignedBy = assignedId,
                    AssignedByUser = assignedByUser,
                    AssignedAt = DateTime.UtcNow
                })
                .ToList();

            if (toRemove.Count > 0)
            {
                _dbContext.UserRoles.RemoveRange(toRemove);
            }

            if (toAdd.Count > 0)
            {
                _dbContext.UserRoles.AddRange(toAdd);
            }

            if (toRemove.Count > 0 || toAdd.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequestDto request)
        {
            var normalizedName = request.Username?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new ArgumentException("Username is required.", nameof(request.Username));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required.", nameof(request.Password));
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            {
                throw new ArgumentException("Valid email is required.", nameof(request.Email));
            }

            var usernameExisted = await _dbContext.Users
                .AnyAsync(r => r.Username.ToLower() == normalizedName.ToLower());

            if (usernameExisted)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            var emailExisted = await _dbContext.Users
                .AnyAsync(r => r.Email!.ToLower() == request.Email.ToLower());

            if (emailExisted)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = normalizedName,
                DisplayName = request.DisplayName?.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password?.Trim()),
                AvatarUrl = request.AvatarUrl,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return MapUserToDto(user);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(r => r.Id == id);
            if (user is null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .OrderBy(r => r.Username)
                .ToListAsync();

            return users.Select(MapUserToDto).ToList();
        }

        public async Task<UserDto> GetUserByIdAsync(Guid id)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }
            return MapUserToDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequestDto request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(r => r.Id == id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                throw new InvalidOperationException("OAuth2 users cannot be edited from this endpoint.");
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            {
                throw new ArgumentException("Valid email is required.", nameof(request.Email));
            }

            var emailExisted = await _dbContext.Users
                .AnyAsync(r => r.Email!.ToLower() == request.Email.ToLower() && r.Id != id);

            if (emailExisted)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            user.Email = request.Email;
            user.DisplayName = request.DisplayName;
            user.AvatarUrl = request.AvatarUrl;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return MapUserToDto(user);
        }

        public async Task<(bool Success, string Message, string? Url)> UploadAvatarAsync(IFormFile? file, HttpRequest request)
        {
            if (file is null || file.Length == 0)
            {
                return (false, "File is required.", null);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) ||
                !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Only image files are allowed (.jpg, .jpeg, .png, .webp, .gif, .svg).", null);
            }

            var webRootPath = _webHostEnvironment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            }

            var relativeFolder = Path.Combine("uploads", "avatars");
            var targetFolder = Path.Combine(webRootPath, relativeFolder);
            Directory.CreateDirectory(targetFolder);

            var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var savePath = Path.Combine(targetFolder, safeFileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            var mediaPath = $"/{relativeFolder.Replace('\\', '/')}/{safeFileName}";
            var configuredBaseUrl = _configuration["Domain:BaseUrl"]?.TrimEnd('/');
            var host = request.Host.HasValue ? request.Host.Value : string.Empty;
            var requestBaseUrl = string.IsNullOrWhiteSpace(host)
                ? string.Empty
                : $"{request.Scheme}://{host}{request.PathBase}".TrimEnd('/');
            var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? configuredBaseUrl
                : requestBaseUrl;
            var absoluteUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? mediaPath
                : $"{baseUrl}{mediaPath}";

            return (true, "Upload successful.", absoluteUrl);
        }

        private UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                MezonUserId = user.MezonUserId,
                Email = user.Email,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                HasPassword = !string.IsNullOrWhiteSpace(user.Password),
                IsOAuthUser = string.IsNullOrWhiteSpace(user.Password),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Guid>> GetUserRoleIdsAsync(Guid userId)
        {
            var userExists = await _dbContext.Users.AnyAsync(r => r.Id == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return await _dbContext.UserRoles
                .AsNoTracking()
                .Where(rp => rp.UserId == userId)
                .Select(rp => rp.RoleId)
                .ToListAsync();
        }
    }
}
