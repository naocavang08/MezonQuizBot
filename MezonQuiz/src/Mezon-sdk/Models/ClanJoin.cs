namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ClanJoin
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

    }
}
