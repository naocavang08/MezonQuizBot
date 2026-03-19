namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MessageActionRow
    {
        [JsonPropertyName("components")]
        public List<MessageComponent>? Components { get; set; }

    }
}
