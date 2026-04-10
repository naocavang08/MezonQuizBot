namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using static Mezon_sdk.Utils.Helper;

    public class UserInitData
    {
        [JsonPropertyName("sender_id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("clan_nick")]
        public string? ClanNick { get; set; }

        [JsonPropertyName("clan_avatar")]
        public string? ClanAvatar { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("dmChannelId")]
        public long DmChannelId { get; set; }

        public static UserInitData FromProtobuf (Mezon.Protobuf.ChannelMessage message, long dmChannelId = 0)
        {
            return new UserInitData
            {
                Id = message.SenderId,
                Username = message.Username ?? "",
                ClanNick = message.ClanNick ?? "",
                ClanAvatar = message.ClanAvatar ?? "",
                Avatar = message.Avatar ?? "",
                DisplayName = message.DisplayName ?? "",
                DmChannelId = dmChannelId
            };
        }

        public Dictionary<string, object?> ToUserDict()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(this, options);

            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, options)
                   ?? new Dictionary<string, object?>();
        }
    }
}
