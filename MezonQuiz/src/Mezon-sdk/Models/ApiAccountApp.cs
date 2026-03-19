namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiAccountApp : MezonBaseModel<ApiAccountApp>
    {
        [JsonPropertyName("appid")]
        public string? Appid { get; set; }

        [JsonPropertyName("appname")]
        public string? Appname { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("vars")]
        public Dictionary<string, string>? Vars { get; set; }

    }
}
