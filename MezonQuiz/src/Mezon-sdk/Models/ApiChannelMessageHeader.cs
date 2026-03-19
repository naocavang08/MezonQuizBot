namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiChannelMessageHeader : MezonBaseModel<ApiChannelMessageHeader>
    {
        [JsonPropertyName("attachment")]
        public string? Attachment { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("mention")]
        public string? Mention { get; set; }

        [JsonPropertyName("reaction")]
        public string? Reaction { get; set; }

        [JsonPropertyName("referece")]
        public string? Referece { get; set; }

        [JsonPropertyName("sender_id")]
        public int? SenderId { get; set; }

        [JsonPropertyName("timestamp_seconds")]
        public int? TimestampSeconds { get; set; }

    }
}
