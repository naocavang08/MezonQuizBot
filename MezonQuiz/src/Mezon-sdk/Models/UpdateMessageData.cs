namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class UpdateMessageData
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

        [JsonPropertyName("content")]
        public object? Content { get; set; }

        [JsonPropertyName("mentions")]
        public List<ApiMessageMention>? Mentions { get; set; }

        [JsonPropertyName("attachments")]
        public List<ApiMessageAttachment>? Attachments { get; set; }

        [JsonPropertyName("hideEditted")]
        public bool? Hideeditted { get; set; }

        [JsonPropertyName("topic_id")]
        public int? TopicId { get; set; }

        [JsonPropertyName("is_update_msg_topic")]
        public bool? IsUpdateMsgTopic { get; set; }

    }
}
