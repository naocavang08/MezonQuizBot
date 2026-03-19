namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ReactMessageData
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

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

        [JsonPropertyName("emoji_id")]
        public int EmojiId { get; set; }

        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("message_sender_id")]
        public int MessageSenderId { get; set; }

        [JsonPropertyName("action_delete")]
        public bool? ActionDelete { get; set; }

    }
}
