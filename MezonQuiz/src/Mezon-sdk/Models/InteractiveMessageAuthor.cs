namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InteractiveMessageAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

    }
}
