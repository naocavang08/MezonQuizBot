using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Application.Auth.Login.Dtos;
using WebApp.Application.Auth.MezonAuth;
using WebApp.Application.Auth.MezonAuth.Dtos;
using WebApp.Data;


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
                _logger.LogWarning("Login failed: user not found or inactive for username {Username}", request.Username);
                return Unauthorized(new { Message = "Username or password is incorrect." });
            }

            if (string.IsNullOrWhiteSpace(user.Password) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                _logger.LogWarning("Login failed: invalid password for username {Username}", request.Username);
                return Unauthorized(new { Message = "Username or password is incorrect." });
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);
            var roles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Name, r.IsSystem })
                .Distinct()
                .ToListAsync();

            var roleNames = roles
                .Select(r => r.Name)
                .ToList();

            var hasSystemRole = roles.Any(r => r.IsSystem);

            var permissionNames = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
                .Join(_dbContext.Permissions, permissionId => permissionId, p => p.Id, (permissionId, p) => p.Resource + "." + p.Action)
                .Distinct()
                .ToListAsync();

            return Ok(new
            {
                Token = token,
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
            });
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
    }
}
