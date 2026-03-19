namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiCreateChannelDescRequest : MezonBaseModel<ApiCreateChannelDescRequest>
    {
        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("channel_private")]
        public int? ChannelPrivate { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

    }
}
