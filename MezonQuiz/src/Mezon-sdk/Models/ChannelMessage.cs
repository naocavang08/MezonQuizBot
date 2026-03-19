namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelMessage
    {
        [JsonPropertyName("id")]
        public int ? Id { get; set; }
        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("sender_id")]
        public int SenderId { get; set; }

        [JsonPropertyName("content")]
        public Dictionary<string, object>? Content { get; set; }

        [JsonPropertyName("mentions")]
        public List<ApiMessageMention>? Mentions { get; set; }

        [JsonPropertyName("attachments")]
        public List<ApiMessageAttachment>? Attachments { get; set; }

        [JsonPropertyName("reactions")]
        public List<ApiMessageReaction>? Reactions { get; set; }

        [JsonPropertyName("references")]
        public List<ApiMessageRef>? References { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("clan_nick")]
        public string? ClanNick { get; set; }

        [JsonPropertyName("clan_avatar")]
        public string? ClanAvatar { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("clan_logo")]
        public string? ClanLogo { get; set; }

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("create_time_seconds")]
        public int? CreateTimeSeconds { get; set; }

        [JsonPropertyName("update_time_seconds")]
        public int? UpdateTimeSeconds { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }

        [JsonPropertyName("hide_editted")]
        public bool? HideEditted { get; set; }

        [JsonPropertyName("topic_id")]
        public int? TopicId { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("referenced_message")]
        public byte[]? ReferencedMessage { get; set; }

    }
}
