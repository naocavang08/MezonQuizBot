namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class UserChannelAddedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_desc")]
        public ApiChannelDescription? ChannelDesc { get; set; }

        [JsonPropertyName("users")]
        public List<UserProfileRedis>? Users { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("caller")]
        public UserProfileRedis? Caller { get; set; }

        [JsonPropertyName("create_time_seconds")]
        public int? CreateTimeSeconds { get; set; }

        [JsonPropertyName("active")]
        public int? Active { get; set; }

    }
}
