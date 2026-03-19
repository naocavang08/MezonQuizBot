namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MessageUserPayLoad
    {
        [JsonPropertyName("userId")]
        public int Userid { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("messOptions")]
        public Dictionary<string, object>? Messoptions { get; set; }

        [JsonPropertyName("attachments")]
        public List<ApiMessageAttachment>? Attachments { get; set; }

        [JsonPropertyName("refs")]
        public List<ApiMessageRef>? Refs { get; set; }

    }
}
