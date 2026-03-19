namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiSession : MezonBaseModel<ApiSession>
    {
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("api_url")]
        public string? ApiUrl { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("ws_url")]
        public string? WsUrl { get; set; }

    }
}
