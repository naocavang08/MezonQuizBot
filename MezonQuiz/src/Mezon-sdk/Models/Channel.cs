namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class Channel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("chanel_label")]
        public string? ChanelLabel { get; set; }

        [JsonPropertyName("presences")]
        public List<Presence>? Presences { get; set; }

        [JsonPropertyName("self")]
        public Presence? SelfPresence { get; set; }

        [JsonPropertyName("clan_logo")]
        public string? ClanLogo { get; set; }

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

    }
}
