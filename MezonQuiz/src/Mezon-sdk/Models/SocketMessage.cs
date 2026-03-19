namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class SocketMessage
    {
        [JsonPropertyName("cid")]
        public string? Cid { get; set; }

    }
}
