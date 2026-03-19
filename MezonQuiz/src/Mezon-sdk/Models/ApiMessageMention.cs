namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiMessageMention : MezonBaseModel<ApiMessageMention>
    {
        [JsonPropertyName("create_time")]
        public string? CreateTime { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("role_id")]
        public int? RoleId { get; set; }

        [JsonPropertyName("rolename")]
        public string? Rolename { get; set; }

        [JsonPropertyName("s")]
        public int? S { get; set; }

        [JsonPropertyName("e")]
        public int? E { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

        [JsonPropertyName("sender_id")]
        public int? SenderId { get; set; }

    }
}
