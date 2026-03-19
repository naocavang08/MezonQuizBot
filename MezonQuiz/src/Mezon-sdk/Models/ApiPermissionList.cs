namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiPermissionList : MezonBaseModel<ApiPermissionList>
    {
        [JsonPropertyName("max_level_permission")]
        public int? MaxLevelPermission { get; set; }

        [JsonPropertyName("permissions")]
        public List<ApiPermission>? Permissions { get; set; }

    }
}
