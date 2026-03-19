namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class UserProfileRedis
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("user_status")]
        public string? UserStatus { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("online")]
        public bool? Online { get; set; }

        [JsonPropertyName("fcm_tokens")]
        public List<FCMTokens>? FcmTokens { get; set; }

        [JsonPropertyName("joined_clans")]
        public List<int>? JoinedClans { get; set; }

        [JsonPropertyName("app_token")]
        public string? AppToken { get; set; }

        [JsonPropertyName("create_time_second")]
        public int? CreateTimeSecond { get; set; }

        [JsonPropertyName("app_url")]
        public string? AppUrl { get; set; }

        [JsonPropertyName("is_bot")]
        public bool? IsBot { get; set; }

        [JsonPropertyName("voip_token")]
        public string? VoipToken { get; set; }

    }
}
