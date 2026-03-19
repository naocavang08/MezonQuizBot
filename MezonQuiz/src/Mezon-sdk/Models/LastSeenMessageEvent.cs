namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class LastSeenMessageEvent
    {
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int Mode { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("timestamp_seconds")]
        public string? TimestampSeconds { get; set; }

    }
}
