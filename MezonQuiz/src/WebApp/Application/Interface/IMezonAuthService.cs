using WebApp.Application.Dtos;
using WebApp.Area.User.Dtos;

namespace WebApp.Application.Interface
{
    public interface IMezonAuthService
    {
        Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request);
    }
}