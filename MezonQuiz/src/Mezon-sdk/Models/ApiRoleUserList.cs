namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiRoleUserList : MezonBaseModel<ApiRoleUserList>
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

        [JsonPropertyName("role_users")]
        public List<RoleUserListRoleUser>? RoleUsers { get; set; }

    }
}
