namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiQuickMenuAccess : MezonBaseModel<ApiQuickMenuAccess>
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("bot_id")]
        public int? BotId { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("menu_name")]
        public string? MenuName { get; set; }

        [JsonPropertyName("background")]
        public string? Background { get; set; }

        [JsonPropertyName("action_msg")]
        public string? ActionMsg { get; set; }

        [JsonPropertyName("menu_type")]
        public int? MenuType { get; set; }

    }
}
