using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebApp.Application.AuditLog.Dtos;
using WebApp.Application.Auth.Login.Dtos;
using WebApp.Application.Auth.MezonAuth;
using WebApp.Application.Auth.MezonAuth.Dtos;
using WebApp.Data;
using WebApp.Domain.Entites;


namespace WebApp.Application.Auth.Login
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IMezonAuthService _mezonAuthService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            ITokenService tokenService,
            IMezonAuthService mezonAuthService,
            AppDbContext dbContext,
            ILogger<LoginController> logger)
        {
            _tokenService = tokenService;
            _mezonAuthService = mezonAuthService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user is null || !user.IsActive)
            {
                await WriteLoginAuditAsync(
                    action: "login.failed",
                    userId: user?.Id,
                    title: "Login Failed",
                    description: $"Username '{request.Username}' not found or inactive.",
                    status: "failed");

                _logger.LogWarning("Login failed: user not found or inactive for username {Username}", request.Username);
                return Unauthorized(new { Message = "Username or password is incorrect." });
            }

            if (string.IsNullOrWhiteSpace(user.Password) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                await WriteLoginAuditAsync(
                    action: "login.failed",
                    userId: user.Id,
                    title: "Login Failed",
                    description: $"Invalid password for username '{request.Username}'.",
                    status: "failed");

                _logger.LogWarning("Login failed: invalid password for username {Username}", request.Username);
                return Unauthorized(new { Message = "Username or password is incorrect." });
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            var authResponse = await BuildAuthResponseAsync(user);

            await WriteLoginAuditAsync(
                action: "login.success",
                userId: user.Id,
                title: "Login Success",
                description: $"User '{user.Username}' logged in successfully.",
                status: "success");

            return Ok(authResponse);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var incomingRefreshToken = request.RefreshToken?.Trim();
            if (string.IsNullOrWhiteSpace(incomingRefreshToken))
            {
                return Unauthorized(new { Message = "Refresh token is required." });
            }

            var tokenHash = HashToken(incomingRefreshToken);
            var storedRefreshToken = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedRefreshToken is null || storedRefreshToken.RevokedAt is not null || storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token." });
            }

            var user = storedRefreshToken.User;
            if (!user.IsActive)
            {
                return Unauthorized(new { Message = "User is inactive." });
            }

            var authResponse = await BuildAuthResponseAsync(user, storedRefreshToken);
            return Ok(authResponse);
        }

        [HttpPost("mezon-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> MezonCallback([FromBody] MezonAuthRequest request)
        {
            var result = await _mezonAuthService.HandleCallbackAsync(request);
            return StatusCode(result.StatusCode, result.Payload);
        }

        [HttpGet("mezon-authorize")]
        [AllowAnonymous]
        public async Task<IActionResult> MezonAuthorize()
        {
            var result = await _mezonAuthService.GetAuthorizeUrlAsync();
            return StatusCode(result.StatusCode, result.Payload);
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, RefreshToken? rotatingToken = null)
        {
            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = HashToken(refreshTokenValue);

            if (rotatingToken is not null)
            {
                rotatingToken.RevokedAt = DateTime.UtcNow;
                rotatingToken.ReplacedByTokenHash = refreshTokenHash;
            }

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            var roles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Name, r.IsSystem })
                .Distinct()
                .ToListAsync();

            var roleNames = roles.Select(r => r.Name).ToList();
            var hasSystemRole = roles.Any(r => r.IsSystem);

            var permissionNames = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
                .Join(_dbContext.Permissions, permissionId => permissionId, p => p.Id, (permissionId, p) => p.Resource + "." + p.Action)
                .Distinct()
                .ToListAsync();

            return new AuthResponseDto
            {
                Token = accessToken.Token,
                RefreshToken = refreshTokenValue,
                ExpiresIn = accessToken.ExpiresIn,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.DisplayName,
                    user.AvatarUrl
                },
                RoleName = roleNames,
                PermissionName = permissionNames,
                HasSystemRole = hasSystemRole
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private async Task WriteLoginAuditAsync(string action, Guid? userId, string title, string description, string status)
        {
            try
            {
                _dbContext.AuditLogs.Add(new WebApp.Domain.Entites.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    User = userId.HasValue ? await _dbContext.Users.FindAsync(userId.Value) : null,
                    Action = action,
                    ResourceType = "auth",
                    ResourceId = userId,
                    Details = new AuditDetailsDto
                    {
                        Title = title,
                        Description = description,
                        Status = status,
                    },
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.UtcNow,
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write login audit log.");
            }
        }
    }
}
