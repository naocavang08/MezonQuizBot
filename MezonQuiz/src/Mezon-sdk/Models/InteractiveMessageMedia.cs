namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InteractiveMessageMedia
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public string? Width { get; set; }

        [JsonPropertyName("height")]
        public string? Height { get; set; }

    }
}
