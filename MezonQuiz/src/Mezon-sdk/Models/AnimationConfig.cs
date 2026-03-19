namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class AnimationConfig
    {
        [JsonPropertyName("url_image")]
        public string? UrlImage { get; set; }

        [JsonPropertyName("url_position")]
        public string? UrlPosition { get; set; }

        [JsonPropertyName("pool")]
        public List<string>? Pool { get; set; }

        [JsonPropertyName("repeat")]
        public int? Repeat { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

    }
}
