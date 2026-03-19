namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ClanProfileUpdatedEvent
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("clan_nick")]
        public string? ClanNick { get; set; }

        [JsonPropertyName("clan_avatar")]
        public string? ClanAvatar { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

    }
}
