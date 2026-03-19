namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InteractiveMessageFooter
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

    }
}
