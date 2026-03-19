namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class RoleUserListRoleUser
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("lang_tag")]
        public string? LangTag { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("online")]
        public bool? Online { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

    }
}
