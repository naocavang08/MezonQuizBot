namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelMessageAck
    {
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("create_time")]
        public string? CreateTime { get; set; }

        [JsonPropertyName("update_time")]
        public string? UpdateTime { get; set; }

        [JsonPropertyName("persistence")]
        public bool? Persistence { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }

    }
}
