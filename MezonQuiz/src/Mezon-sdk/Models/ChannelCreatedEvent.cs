namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelCreatedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("channel_private")]
        public int? ChannelPrivate { get; set; }

        [JsonPropertyName("channel_type")]
        public int? ChannelType { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("app_id")]
        public int? AppId { get; set; }

        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("channel_avatar")]
        public string? ChannelAvatar { get; set; }

    }
}
