using System.ComponentModel.DataAnnotations;
namespace WebApp.Area.User.Dtos
{
    public class MezonAuthRequest
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }
}