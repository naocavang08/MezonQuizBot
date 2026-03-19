namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InteractiveMessageProps
    {
        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("author")]
        public InteractiveMessageAuthor? Author { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnail")]
        public InteractiveMessageMedia? Thumbnail { get; set; }

        [JsonPropertyName("fields")]
        public List<InteractiveMessageField>? Fields { get; set; }

        [JsonPropertyName("image")]
        public InteractiveMessageMedia? Image { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("footer")]
        public InteractiveMessageFooter? Footer { get; set; }

    }
}
