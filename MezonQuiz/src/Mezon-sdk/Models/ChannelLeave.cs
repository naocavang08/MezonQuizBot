namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelLeave
    {
        [JsonPropertyName("channel_leave")]
        public Dictionary<string, object>? Channel_leave { get; set; }

    }
}
