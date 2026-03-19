namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiMessageDeleted : MezonBaseModel<ApiMessageDeleted>
    {
        [JsonPropertyName("deletor")]
        public string? Deletor { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

    }
}
