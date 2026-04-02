using System.Text.Json.Serialization;

namespace WebApp.Application.ManageQuizSession.Dtos
{
    public class DataJsonDto
    {
        [property: JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [property: JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [property: JsonPropertyName("avatar")]
        public string Avatar { get; set; } = string.Empty;
    }

    public class SessionLinksDto
    {
        public string DeepLink { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
