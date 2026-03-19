namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiMessageRef : MezonBaseModel<ApiMessageRef>
    {
        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

        [JsonPropertyName("message_ref_id")]
        public int MessageRefId { get; set; }

        [JsonPropertyName("ref_type")]
        public int? RefType { get; set; }

        [JsonPropertyName("message_sender_id")]
        public int MessageSenderId { get; set; }

        [JsonPropertyName("message_sender_username")]
        public string? MessageSenderUsername { get; set; }

        [JsonPropertyName("message_sender_avatar")]
        public string? MessageSenderAvatar { get; set; }

        [JsonPropertyName("message_sender_clan_nick")]
        public string? MessageSenderClanNick { get; set; }

        [JsonPropertyName("message_sender_display_name")]
        public string? MessageSenderDisplayName { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("has_attachment")]
        public bool? HasAttachment { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

    }
}
