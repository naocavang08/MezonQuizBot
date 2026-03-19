namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class VoiceEndedEvent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("voice_channel_id")]
        public string? VoiceChannelId { get; set; }

    }
}
