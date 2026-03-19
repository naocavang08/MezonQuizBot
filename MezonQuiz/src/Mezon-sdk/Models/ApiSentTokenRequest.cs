namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiSentTokenRequest : MezonBaseModel<ApiSentTokenRequest>
    {
        [JsonPropertyName("receiver_id")]
        public int ReceiverId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("sender_id")]
        public int? SenderId { get; set; }

        [JsonPropertyName("sender_name")]
        public string? SenderName { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("extra_attribute")]
        public string? ExtraAttribute { get; set; }

        [JsonPropertyName("mmn_extra_info")]
        public Dictionary<string, object>? MmnExtraInfo { get; set; }

        [JsonPropertyName("timestamp")]
        public int? Timestamp { get; set; }

    }
}
