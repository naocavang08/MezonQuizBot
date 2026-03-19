namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class StreamingJoinedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("participant")]
        public string? Participant { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("streaming_channel_label")]
        public string? StreamingChannelLabel { get; set; }

        [JsonPropertyName("streaming_channel_id")]
        public int StreamingChannelId { get; set; }

    }
}
