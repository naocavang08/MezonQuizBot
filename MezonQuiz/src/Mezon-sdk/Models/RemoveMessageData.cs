namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class RemoveMessageData
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int Mode { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("topic_id")]
        public int? TopicId { get; set; }

    }
}
