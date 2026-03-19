namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class EphemeralMessageData
    {
        [JsonPropertyName("receiver_id")]
        public int ReceiverId { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int Mode { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("content")]
        public object? Content { get; set; }

        [JsonPropertyName("mentions")]
        public List<ApiMessageMention>? Mentions { get; set; }

        [JsonPropertyName("attachments")]
        public List<ApiMessageAttachment>? Attachments { get; set; }

        [JsonPropertyName("references")]
        public List<ApiMessageRef>? References { get; set; }

        [JsonPropertyName("anonymous_message")]
        public bool? AnonymousMessage { get; set; }

        [JsonPropertyName("mention_everyone")]
        public bool? MentionEveryone { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("topic_id")]
        public int? TopicId { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

    }
}
