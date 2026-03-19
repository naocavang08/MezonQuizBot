namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MessageComponent
    {
        [JsonPropertyName("type")]
        public object? Type { get; set; }

        [JsonPropertyName("id")]
        public string? ComponentId { get; set; }

        [JsonPropertyName("component")]
        public Dictionary<string, object>? Component { get; set; }

    }
}
