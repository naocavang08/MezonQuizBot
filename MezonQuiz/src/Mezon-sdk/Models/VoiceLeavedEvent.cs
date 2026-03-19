namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class VoiceLeavedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("voice_channel_id")]
        public int VoiceChannelId { get; set; }

        [JsonPropertyName("voice_user_id")]
        public int VoiceUserId { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

    }
}
