namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class VoiceJoinedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("voice_channel_id")]
        public int VoiceChannelId { get; set; }

        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("participant")]
        public string? Participant { get; set; }

        [JsonPropertyName("voice_channel_label")]
        public string? VoiceChannelLabel { get; set; }

        [JsonPropertyName("last_screenshot")]
        public string? LastScreenshot { get; set; }

    }
}
