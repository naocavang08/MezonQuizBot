namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ReactMessagePayload
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("emoji_id")]
        public int EmojiId { get; set; }

        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("action_delete")]
        public bool? ActionDelete { get; set; }

    }
}
