namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class SelectFieldOption
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

    }
}
