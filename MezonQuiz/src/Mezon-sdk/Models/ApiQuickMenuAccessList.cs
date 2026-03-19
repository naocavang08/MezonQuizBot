namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiQuickMenuAccessList : MezonBaseModel<ApiQuickMenuAccessList>
    {
        [JsonPropertyName("list_menus")]
        public List<ApiQuickMenuAccess>? ListMenus { get; set; }

    }
}
