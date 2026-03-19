namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MessageTypingEvent
    {
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("sender_id")]
        public int SenderId { get; set; }

        [JsonPropertyName("sender_username")]
        public string? SenderUsername { get; set; }

        [JsonPropertyName("sender_display_name")]
        public string? SenderDisplayName { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

    }
}
