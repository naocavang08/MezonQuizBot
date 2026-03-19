namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DropdownBoxSelected
    {
        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("selectbox_id")]
        public string? SelectboxId { get; set; }

        [JsonPropertyName("sender_id")]
        public int SenderId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("values")]
        public List<string>? Values { get; set; }

    }
}
