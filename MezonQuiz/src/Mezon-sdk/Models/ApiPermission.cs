namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiPermission : MezonBaseModel<ApiPermission>
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("active")]
        public int? Active { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("scope")]
        public int? Scope { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

    }
}
