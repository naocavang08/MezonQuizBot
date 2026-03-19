namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class VoiceStartedEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("voice_channel_id")]
        public int VoiceChannelId { get; set; }

    }
}
