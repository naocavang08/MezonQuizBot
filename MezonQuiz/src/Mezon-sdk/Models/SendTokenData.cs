namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class SendTokenData
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("extra_attribute")]
        public string? ExtraAttribute { get; set; }

    }
}
