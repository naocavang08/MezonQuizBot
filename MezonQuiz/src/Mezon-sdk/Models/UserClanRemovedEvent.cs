namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class UserClanRemovedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

    }
}
