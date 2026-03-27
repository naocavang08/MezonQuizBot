namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MarkdownOnMessage : StartEndIndex
    {
        [JsonPropertyName("type")]
        public EMarkdownType? Type { get; set; }

    }
}
