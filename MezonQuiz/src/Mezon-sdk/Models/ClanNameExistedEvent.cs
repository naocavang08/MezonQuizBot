namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ClanNameExistedEvent
    {
        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("exist")]
        public bool Exist { get; set; }

    }
}
