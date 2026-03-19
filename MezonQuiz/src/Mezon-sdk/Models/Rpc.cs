namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class Rpc
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("payload")]
        public object? Payload { get; set; }

    }
}
