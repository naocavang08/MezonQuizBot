namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiRoleList : MezonBaseModel<ApiRoleList>
    {
        [JsonPropertyName("cacheable_cursor")]
        public string? CacheableCursor { get; set; }

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }

        [JsonPropertyName("prev_cursor")]
        public string? PrevCursor { get; set; }

        [JsonPropertyName("roles")]
        public List<ApiRole>? Roles { get; set; }

    }
}
