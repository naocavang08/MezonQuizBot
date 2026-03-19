namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InputFieldOption
    {
        [JsonPropertyName("defaultValue")]
        public object? Defaultvalue { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("textarea")]
        public bool? Textarea { get; set; }

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }

    }
}
