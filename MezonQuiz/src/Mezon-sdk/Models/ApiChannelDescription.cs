namespace Mezon_sdk.Models
{
    using Google.Protobuf;
    using Mezon.Protobuf;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ApiChannelDescription : MezonBaseModel<ApiChannelDescription>
    {
        [JsonPropertyName("active")]
        public int? Active { get; set; }

        [JsonPropertyName("avatars")]
        public List<string>? Avatars { get; set; }

        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("channel_avatar")]
        public List<string>? ChannelAvatar { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("channel_private")]
        public int? ChannelPrivate { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("count_mess_unread")]
        public int? CountMessUnread { get; set; }

        [JsonPropertyName("create_time_seconds")]
        public int? CreateTimeSeconds { get; set; }

        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        [JsonPropertyName("creator_name")]
        public string? CreatorName { get; set; }

        [JsonPropertyName("display_names")]
        public List<string>? DisplayNames { get; set; }

        [JsonPropertyName("last_pin_message")]
        public string? LastPinMessage { get; set; }

        [JsonPropertyName("last_seen_message")]
        public ApiChannelMessageHeader? LastSeenMessage { get; set; }

        [JsonPropertyName("last_sent_message")]
        public ApiChannelMessageHeader? LastSentMessage { get; set; }

        [JsonPropertyName("meeting_code")]
        public string? MeetingCode { get; set; }

        [JsonPropertyName("meeting_uri")]
        public string? MeetingUri { get; set; }

        [JsonPropertyName("onlines")]
        public List<bool>? Onlines { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("update_time_seconds")]
        public int? UpdateTimeSeconds { get; set; }

        [JsonPropertyName("user_id")]
        public List<int>? UserId { get; set; }

        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

        [JsonPropertyName("usernames")]
        public List<string>? Usernames { get; set; }

        public static ApiChannelDescription FromProtobuf (ChannelDescription message)
        {
            string json = JsonFormatter.Default.Format(message);

            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                       ?? new Dictionary<string, object>();

            // Fix field "type" if needed
            if (message.Type != default)
            {
                dict["type"] = message.Type;
            }

            // Dictionary -> Model
            return FromDictionary(dict);
        }

        private static ApiChannelDescription FromDictionary(Dictionary<string, object> dict)
        {
            return new ApiChannelDescription
            {
                Active = dict.TryGetValue("active", out var active) ? Convert.ToInt32(active) : (int?)null,
                Avatars = dict.TryGetValue("avatars", out var avatars) ? JsonSerializer.Deserialize<List<string>>(avatars.ToString() ?? "[]") : null,
                CategoryId = dict.TryGetValue("category_id", out var categoryId) ? Convert.ToInt32(categoryId) : (int?)null,
                CategoryName = dict.TryGetValue("category_name", out var categoryName) ? categoryName.ToString() : null,
                ChannelAvatar = dict.TryGetValue("channel_avatar", out var channelAvatar) ? JsonSerializer.Deserialize<List<string>>(channelAvatar.ToString() ?? "[]") : null,
                ChannelId = dict.TryGetValue("channel_id", out var channelId) ? Convert.ToInt32(channelId) : (int?)null,
                ChannelLabel = dict.TryGetValue("channel_label", out var channelLabel) ? channelLabel.ToString() : null,
                ChannelPrivate = dict.TryGetValue("channel_private", out var channelPrivate) ? Convert.ToInt32(channelPrivate) : (int?)null,
                ClanId = dict.TryGetValue("clan_id", out var clanId) ? Convert.ToInt32(clanId) : (int?)null,
                ClanName = dict.TryGetValue("clan_name", out var clanName) ? clanName.ToString() : null,
                CountMessUnread = dict.TryGetValue("count_mess_unread", out var countMessUnread) ? Convert.ToInt32(countMessUnread) : (int?)null,
                CreateTimeSeconds = dict.TryGetValue("create_time_seconds", out var createTimeSeconds) ? Convert.ToInt32(createTimeSeconds) : (int?)null,
                CreatorId = dict.TryGetValue("creator_id", out var creatorId) ? Convert.ToInt32(creatorId) : (int?)null,
                CreatorName = dict.TryGetValue("creator_name", out var creatorName) ? creatorName.ToString() : null,
                DisplayNames = dict.TryGetValue("display_names", out var displayNames) ? JsonSerializer.Deserialize<List<string>>(displayNames.ToString() ?? "[]") : null,
                LastPinMessage = dict.TryGetValue("last_pin_message", out var lastPinMessage) ? lastPinMessage.ToString() : null,
                LastSeenMessage = dict.TryGetValue("last_seen_message", out var lastSeenMessage) ? JsonSerializer.Deserialize<ApiChannelMessageHeader>(lastSeenMessage.ToString() ?? "{}") : null,
                LastSentMessage = dict.TryGetValue("last_sent_message", out var lastSentMessage) ? JsonSerializer.Deserialize<ApiChannelMessageHeader>(lastSentMessage.ToString() ?? "{}") : null,
                MeetingCode = dict.TryGetValue("meeting_code", out var meetingCode) ? meetingCode.ToString() : null,
                MeetingUri = dict.TryGetValue("meeting_uri", out var meetingUri) ? meetingUri.ToString() : null,
                Onlines = dict.TryGetValue("onlines", out var onlines) ? JsonSerializer.Deserialize<List<bool>>(onlines.ToString() ?? "[]") : null,
                ParentId = dict.TryGetValue("parent_id", out var parentId) ? Convert.ToInt32(parentId) : (int?)null,
                Status = dict.TryGetValue("status", out var status) ? Convert.ToInt32(status) : (int?)null,
                Type = dict.TryGetValue("type", out var type) ? Convert.ToInt32(type) : (int?)null,
                UpdateTimeSeconds = dict.TryGetValue("update_time_seconds", out var updateTimeSeconds) ? Convert.ToInt32(updateTimeSeconds) : (int?)null,
                UserId = dict.TryGetValue("user_id", out var userId) ? JsonSerializer.Deserialize<List<int>>(userId.ToString() ?? "[]") : null,
                UserIds = dict.TryGetValue("user_ids", out var userIds) ? JsonSerializer.Deserialize<List<int>>(userIds.ToString() ?? "[]") : null,
                Usernames = dict.TryGetValue("usernames", out var usernames) ? JsonSerializer.Deserialize<List<string>>(usernames.ToString() ?? "[]") : null
            };
        }
    }
}
