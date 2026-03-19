namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class UserInitData
    {
        [JsonPropertyName("sender_id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("clan_nick")]
        public string? ClanNick { get; set; }

        [JsonPropertyName("clan_avatar")]
        public string? ClanAvatar { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("dmChannelId")]
        public int DmChannelId { get; set; }

    }
}
