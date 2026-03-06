using System.ComponentModel.DataAnnotations;
namespace WebApp.Controllers.Dtos
{
    public class MezonAuthRequest
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }
}