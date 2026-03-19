namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelMessageSend
    {
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int Mode { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("content")]
        public object? Content { get; set; }

        [JsonPropertyName("mentions")]
        public List<ApiMessageMention>? Mentions { get; set; }

        [JsonPropertyName("attachments")]
        public List<ApiMessageAttachment>? Attachments { get; set; }

        [JsonPropertyName("references")]
        public List<ApiMessageRef>? References { get; set; }

    }
}
