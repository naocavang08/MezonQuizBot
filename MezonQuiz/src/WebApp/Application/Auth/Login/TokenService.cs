using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using WebApp.Application.Auth.Login.Dtos;
using WebApp.Domain.Entites;

namespace WebApp.Application.Auth.Login
{
    public class TokenService : ITokenService
    {
        private const int DefaultAccessTokenExpirationMinutes = 60;
        private const int DefaultRefreshTokenExpirationDays = 30;
        private readonly ILogger<TokenService> _logger;
        private readonly IConfiguration _configuration;

        public TokenService(ILogger<TokenService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public AccessTokenResult CreateAccessToken(User user)
        {
            var expirationMinutes = GetOptionalIntJwtSetting("AccessTokenExpirationMinutes", DefaultAccessTokenExpirationMinutes);
            var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
            var token = CreateJwtToken(
                CreateClaims(user),
                CreateSigningCredentials(),
                expiration
            );
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValue = tokenHandler.WriteToken(token);

            _logger.LogInformation("JWT Token created");

            return new AccessTokenResult
            {
                Token = tokenValue,
                ExpiresIn = (int)Math.Max(0, (expiration - DateTime.UtcNow).TotalSeconds),
                ExpiresAt = expiration
            };
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Base64UrlEncoder.Encode(randomBytes);
        }

        public DateTime GetRefreshTokenExpiration()
        {
            var refreshExpirationDays = GetOptionalIntJwtSetting("RefreshTokenExpirationDays", DefaultRefreshTokenExpirationDays);
            return DateTime.UtcNow.AddDays(refreshExpirationDays);
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

        private int GetOptionalIntJwtSetting(string key, int defaultValue)
        {
            var value = _configuration[$"JwtTokenSettings:{key}"];
            return int.TryParse(value, out var parsedValue) && parsedValue > 0
                ? parsedValue
                : defaultValue;
        }
    }
}
