namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiChannelDescList : MezonBaseModel<ApiChannelDescList>
    {
        [JsonPropertyName("channeldesc")]
        public List<ApiChannelDescription>? Channeldesc { get; set; }

        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

    }
}
