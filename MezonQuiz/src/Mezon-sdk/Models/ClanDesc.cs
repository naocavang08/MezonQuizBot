namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ClanDesc
    {
        [JsonPropertyName("banner")]
        public string? Banner { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        [JsonPropertyName("logo")]
        public string? Logo { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

    }
}
