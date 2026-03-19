namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelJoin
    {
        [JsonPropertyName("channel_join")]
        public Dictionary<string, object>? Channel_join { get; set; }

    }
}
