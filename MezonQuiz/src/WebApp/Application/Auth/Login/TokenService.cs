using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApp.Domain.Entites;

namespace WebApp.Application.Auth.Login
{
    public class TokenService : ITokenService
    {
        private const int ExpirationMinutes = 60;
        private readonly ILogger<TokenService> _logger;
        private readonly IConfiguration _configuration;

        public TokenService(ILogger<TokenService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public string CreateToken(User user)
        {
            var expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
            var token = CreateJwtToken(
                CreateClaims(user),
                CreateSigningCredentials(),
                expiration
            );
            var tokenHandler = new JwtSecurityTokenHandler();

            _logger.LogInformation("JWT Token created");

            return tokenHandler.WriteToken(token);
        }

        private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials,
            DateTime expiration) =>
            new(
                GetRequiredJwtSetting("ValidIssuer"),
                GetRequiredJwtSetting("ValidAudience"),
                claims,
                expires: expiration,
                signingCredentials: credentials
            );

        private List<Claim> CreateClaims(User user)
        {
            var jwtSub = GetRequiredJwtSetting("JwtRegisteredClaimNamesSub");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, jwtSub),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            return claims;
        }

        private SigningCredentials CreateSigningCredentials()
        {
            var symmetricSecurityKey = GetRequiredJwtSetting("SymmetricSecurityKey");

            return new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(symmetricSecurityKey)
                ),
                SecurityAlgorithms.HmacSha256
            );
        }

        private string GetRequiredJwtSetting(string key)
        {
            var value = _configuration[$"JwtTokenSettings:{key}"];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing JwtTokenSettings:{key} configuration.");
            }

            return value;
        }
    }
}
