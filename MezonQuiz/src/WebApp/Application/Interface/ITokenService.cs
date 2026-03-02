using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApp.Domain.Entites;

namespace WebApp.Application.Interface
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
