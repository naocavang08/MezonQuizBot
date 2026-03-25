using WebApp.Application.Auth.MezonAuth.Dtos;

namespace WebApp.Application.Auth.MezonAuth
{
    public interface IMezonAuthService
    {
        Task<MezonCallbackResult> GetAuthorizeUrlAsync();
        Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request);
    }
}
