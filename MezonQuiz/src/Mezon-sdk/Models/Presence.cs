namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class Presence
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("node")]
        public string? Node { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

    }
}
