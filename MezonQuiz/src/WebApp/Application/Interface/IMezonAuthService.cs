using WebApp.Application.Dtos;
using WebApp.Controllers.Dtos;

namespace WebApp.Application.Interface
{
    public interface IMezonAuthService
    {
        Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request);
    }
}