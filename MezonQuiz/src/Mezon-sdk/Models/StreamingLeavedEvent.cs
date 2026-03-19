namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class StreamingLeavedEvent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("streaming_channel_id")]
        public string? StreamingChannelId { get; set; }

        [JsonPropertyName("streaming_user_id")]
        public string? StreamingUserId { get; set; }

    }
}
