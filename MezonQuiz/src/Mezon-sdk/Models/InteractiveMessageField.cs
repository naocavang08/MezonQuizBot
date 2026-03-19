namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InteractiveMessageField
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("inline")]
        public bool? Inline { get; set; }

        [JsonPropertyName("options")]
        public List<object>? Options { get; set; }

        [JsonPropertyName("inputs")]
        public Dictionary<string, object>? Inputs { get; set; }

        [JsonPropertyName("max_options")]
        public int? MaxOptions { get; set; }

    }
}
