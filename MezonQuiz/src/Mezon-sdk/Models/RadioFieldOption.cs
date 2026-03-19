namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class RadioFieldOption
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("style")]
        public int? Style { get; set; }

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }

    }
}
