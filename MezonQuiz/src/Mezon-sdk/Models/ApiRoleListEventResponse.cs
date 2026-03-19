namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiRoleListEventResponse : MezonBaseModel<ApiRoleListEventResponse>
    {
        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

        [JsonPropertyName("limit")]
        public string? Limit { get; set; }

        [JsonPropertyName("roles")]
        public ApiRoleList? Roles { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

    }
}
