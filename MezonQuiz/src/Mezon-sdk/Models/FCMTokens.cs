namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class FCMTokens
    {
        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("token_id")]
        public string? TokenId { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

    }
}
