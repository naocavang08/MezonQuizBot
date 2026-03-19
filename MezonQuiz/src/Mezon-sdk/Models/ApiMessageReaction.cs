namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiMessageReaction : MezonBaseModel<ApiMessageReaction>
    {
        [JsonPropertyName("action")]
        public bool? Action { get; set; }

        [JsonPropertyName("emoji_id")]
        public int? EmojiId { get; set; }

        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("sender_id")]
        public int? SenderId { get; set; }

        [JsonPropertyName("sender_name")]
        public string? SenderName { get; set; }

        [JsonPropertyName("sender_avatar")]
        public string? SenderAvatar { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

    }
}
