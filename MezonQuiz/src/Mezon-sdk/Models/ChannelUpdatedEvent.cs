namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelUpdatedEvent
    {
        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("channel_type")]
        public int? ChannelType { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("meeting_code")]
        public string? MeetingCode { get; set; }

        [JsonPropertyName("is_error")]
        public bool? IsError { get; set; }

        [JsonPropertyName("channel_private")]
        public bool? ChannelPrivate { get; set; }

        [JsonPropertyName("app_id")]
        public int? AppId { get; set; }

        [JsonPropertyName("e2ee")]
        public int? E2Ee { get; set; }

        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("age_restricted")]
        public int? AgeRestricted { get; set; }

        [JsonPropertyName("active")]
        public int? Active { get; set; }

        [JsonPropertyName("count_mess_unread")]
        public int? CountMessUnread { get; set; }

        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

        [JsonPropertyName("role_ids")]
        public List<int>? RoleIds { get; set; }

        [JsonPropertyName("channel_avatar")]
        public string? ChannelAvatar { get; set; }

    }
}
