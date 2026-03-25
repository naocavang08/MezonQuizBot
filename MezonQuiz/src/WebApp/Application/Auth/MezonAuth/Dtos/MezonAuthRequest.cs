using System.ComponentModel.DataAnnotations;
namespace WebApp.Application.Auth.MezonAuth.Dtos
{
    public class MezonAuthRequest
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }
}
