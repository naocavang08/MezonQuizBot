namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class UserChannelRemoved
    {
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

        [JsonPropertyName("channel_type")]
        public int ChannelType { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

    }
}
