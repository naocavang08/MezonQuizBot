namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class TokenSentEvent
    {
        [JsonPropertyName("receiver_id")]
        public int ReceiverId { get; set; }

        [JsonPropertyName("sender_id")]
        public int? SenderId { get; set; }

        [JsonPropertyName("sender_name")]
        public string? SenderName { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("extra_attribute")]
        public string? ExtraAttribute { get; set; }

        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

    }
}
