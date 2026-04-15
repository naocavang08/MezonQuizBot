using WebApp.Application.Auth.Login.Dtos;
using WebApp.Domain.Entites;

namespace WebApp.Application.Auth.Login
{
    public interface ITokenService
    {
        AccessTokenResult CreateAccessToken(User user);
        string GenerateRefreshToken();
        DateTime GetRefreshTokenExpiration();
    }
}
