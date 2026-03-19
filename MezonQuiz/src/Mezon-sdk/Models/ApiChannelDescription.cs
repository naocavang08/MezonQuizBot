namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
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

    }
}
