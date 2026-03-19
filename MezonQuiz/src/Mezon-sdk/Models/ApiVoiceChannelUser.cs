namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiVoiceChannelUser : MezonBaseModel<ApiVoiceChannelUser>
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("participant")]
        public string? Participant { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

    }
}
