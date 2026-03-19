namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class GiveCoffeeEvent
    {
        [JsonPropertyName("sender_id")]
        public int SenderId { get; set; }

        [JsonPropertyName("receiver_id")]
        public int ReceiverId { get; set; }

        [JsonPropertyName("token_count")]
        public int TokenCount { get; set; }

        [JsonPropertyName("message_ref_id")]
        public int MessageRefId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

    }
}
