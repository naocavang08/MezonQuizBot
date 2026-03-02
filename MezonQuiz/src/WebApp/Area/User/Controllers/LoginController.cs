using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Application.Interface;
using WebApp.Area.User.Dtos;
using WebApp.Data;

namespace WebApp.Area.User.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<LoginController> _logger;
        public LoginController(
            ITokenService tokenService,
            AppDbContext dbContext,
            ILogger<LoginController> logger)
        {
            _tokenService = tokenService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
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
                return Unauthorized(new { Message = "Username hoặc password không đúng." });
            }

            if (string.IsNullOrWhiteSpace(user.Password) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                _logger.LogWarning("Login failed: invalid password for username {Username}", request.Username);
                return Unauthorized(new { Message = "Username hoặc password không đúng." });
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);

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
                }
            });
        }
    }
}
