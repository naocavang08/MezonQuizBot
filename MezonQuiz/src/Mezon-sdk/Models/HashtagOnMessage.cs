namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class HashtagOnMessage
    {
        [JsonPropertyName("channelid")]
        public int? ChannelId { get; set; }

    }
}
