namespace Mezon_sdk.Models
{
    using Mezon.Protobuf;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiQuickMenuAccessList : MezonBaseModel<ApiQuickMenuAccessList>
    {
        [JsonPropertyName("list_menus")]
        public List<ApiQuickMenuAccess>? ListMenus { get; set; }

        public static ApiQuickMenuAccessList FromProtobuf (QuickMenuAccessList message)
        {
            var menus = new List<ApiQuickMenuAccess>();

            foreach (var menu in message.ListMenus)
            {
                var item = ApiQuickMenuAccess.FromProtobuf(menu);
                if (item is not null)
                {
                    menus.Add(item);
                }
            }

            return new ApiQuickMenuAccessList
            {
                ListMenus = menus.Count > 0 ? menus : null
            };
        }
    }
}
