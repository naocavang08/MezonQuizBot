namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ButtonMessage
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("disable")]
        public bool? Disable { get; set; }

        [JsonPropertyName("style")]
        public int? Style { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

    }
}
