namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MarkdownOnMessage
    {
        [JsonPropertyName("type")]
        public EMarkdownType? Type { get; set; }

    }
}
