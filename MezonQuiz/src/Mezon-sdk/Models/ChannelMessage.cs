namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using static Mezon_sdk.Utils.Helper;

    public class ChannelMessage
    {
        [JsonIgnore]
        public int Id => MessageId;
        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("clan_id")]
        public int ClanId { get; set; }

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        [JsonPropertyName("sender_id")]
        public int SenderId { get; set; }

        [JsonPropertyName("content")]
        public Dictionary<string, object>? Content { get; set; }

        [JsonPropertyName("mentions")]
        public List<ApiMessageMention>? Mentions { get; set; }

        [JsonPropertyName("attachments")]
        public List<ApiMessageAttachment>? Attachments { get; set; }

        [JsonPropertyName("reactions")]
        public List<ApiMessageReaction>? Reactions { get; set; }

        [JsonPropertyName("references")]
        public List<ApiMessageRef>? References { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("clan_nick")]
        public string? ClanNick { get; set; }

        [JsonPropertyName("clan_avatar")]
        public string? ClanAvatar { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("clan_logo")]
        public string? ClanLogo { get; set; }

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("create_time_seconds")]
        public int? CreateTimeSeconds { get; set; }

        [JsonPropertyName("update_time_seconds")]
        public int? UpdateTimeSeconds { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }

        [JsonPropertyName("hide_editted")]
        public bool? HideEditted { get; set; }

        [JsonPropertyName("topic_id")]
        public int? TopicId { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("referenced_message")]
        public byte[]? ReferencedMessage { get; set; }

        public static ChannelMessage FromProtobuf(Mezon.Protobuf.ChannelMessage message)
        {
            return new ChannelMessage
            {
                MessageId = ToInt(message.MessageId) ?? 0,
                ClanId = ToInt(message.ClanId) ?? 0,
                ChannelId = ToInt(message.ChannelId) ?? 0,
                SenderId = ToInt(message.SenderId) ?? 0,

                Content = SafeJsonParse<Dictionary<string, object>>(message.Content, new()),

                Mentions = DecodeMentions(message.Mentions),
                Attachments = DecodeAttachments(message.Attachments),
                Reactions = DecodeReactions(message.Reactions),
                References = DecodeReferences(message.References),

                Username = message.Username,
                Avatar = message.Avatar,
                DisplayName = message.DisplayName,
                ClanNick = message.ClanNick,
                ClanAvatar = message.ClanAvatar,
                ChannelLabel = message.ChannelLabel,
                ClanLogo = message.ClanLogo,
                CategoryName = message.CategoryName,

                CreateTimeSeconds = ToInt(message.CreateTimeSeconds),
                UpdateTimeSeconds = ToInt(message.UpdateTimeSeconds),
                Mode = ToInt(message.Mode),
                IsPublic = message.IsPublic,
                HideEditted = message.HideEditted,
                TopicId = ToInt(message.TopicId),
                Code = ToInt(message.Code),
                ReferencedMessage = message.ReferencedMessage.ToByteArray()
            };
        }

        private static T SafeJsonParse<T>(string? json, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(json))
                return defaultValue;

            try
            {
                return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static List<ApiMessageMention> DecodeMentions(Google.Protobuf.ByteString data)
        {
            if (data == null || data.Length == 0) return new();

            try
            {
                var list = Mezon.Protobuf.MessageMentionList.Parser.ParseFrom(data);

                return list.Mentions.Select(m => new ApiMessageMention
                {
                    Id = ToInt(m.Id) ?? 0,
                    UserId = ToInt(m.UserId) ?? 0,
                    Username = m.Username,
                    RoleId = ToInt(m.RoleId) ?? 0,
                    Rolename = m.Rolename,
                    S = ToInt(m.S) ?? 0,
                    E = ToInt(m.E) ?? 0
                }).ToList();
            }
            catch
            {
                return new();
            }
        }

        private static List<ApiMessageAttachment> DecodeAttachments(Google.Protobuf.ByteString data)
        {
            if (data == null || data.Length == 0) return new();

            try
            {
                var list = Mezon.Protobuf.MessageAttachmentList.Parser.ParseFrom(data);

                return list.Attachments.Select(a => new ApiMessageAttachment
                {
                    Filename = a.Filename,
                    Filetype = a.Filetype,
                    Height = a.Height,
                    Size = a.Size,
                    Url = a.Url,
                    Width = a.Width,
                    Thumbnail = a.Thumbnail,
                    Duration = a.Duration
                }).ToList();
            }
            catch
            {
                return new();
            }
        }

        private static List<ApiMessageReaction> DecodeReactions(Google.Protobuf.ByteString data)
        {
            if (data == null || data.Length == 0) return new();

            try
            {
                var list = Mezon.Protobuf.MessageReactionList.Parser.ParseFrom(data);

                return list.Reactions.Select(r => new ApiMessageReaction
                {
                    Action = r.Action,
                    EmojiId = ToInt(r.EmojiId) ?? 0,
                    Emoji = r.Emoji,
                    Id = ToInt(r.Id) ?? 0,
                    SenderId = ToInt(r.SenderId) ?? 0,
                    SenderName = r.SenderName,
                    SenderAvatar = r.SenderAvatar,
                    Count = r.Count
                }).ToList();
            }
            catch
            {
                return new();
            }
        }

        private static List<ApiMessageRef> DecodeReferences(Google.Protobuf.ByteString data)
        {
            if (data == null || data.Length == 0) return new();

            try
            {
                var list = Mezon.Protobuf.MessageRefList.Parser.ParseFrom(data);


                return list.Refs.Select(r => new ApiMessageRef
                {
                    MessageId = ToInt(r.MessageId) ?? 0,
                    MessageRefId = ToInt(r.MessageRefId) ?? 0,
                    RefType = r.RefType,
                    MessageSenderId = ToInt(r.MessageSenderId) ?? 0,
                    MessageSenderUsername = r.MessageSenderUsername,
                    MessageSenderDisplayName = r.MessageSenderDisplayName,
                    MessageSenderAvatar = r.MessageSenderAvatar,
                    HasAttachment = r.HasAttachment,
                    MessageSenderClanNick = r.MessageSenderClanNick,
                    Content = r.Content
                }).ToList();
            }
            catch
            {
                return new();
            }
        }

        public Dictionary<string, object?> ToMessageDict()
        {
            var json = JsonSerializer.Serialize(this);

            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                   ?? new Dictionary<string, object?>();
        }

        public Dictionary<string, object> ToDbDict()
        {
            return new Dictionary<string, object>
            {
                ["message_id"] = MessageId,
                ["clan_id"] = ClanId,
                ["channel_id"] = ChannelId,
                ["sender_id"] = SenderId,
                ["content"] = Content ?? new(),
                ["reactions"] = Reactions?.Select(r => r).ToList() ?? new(),
                ["mentions"] = Mentions?.Select(m => m).ToList() ?? new(),
                ["attachments"] = Attachments?.Select(a => a).ToList() ?? new(),
                ["references"] = References?.Select(r => r).ToList() ?? new(),
                ["create_time_seconds"] = CreateTimeSeconds ?? 0
            };
        }

        public static ChannelMessage FromDictionary(Dictionary<string, object> dict)
        {
            var reactionsData = ParseJson<List<object>>(dict, "reactions") ?? new List<object>();
            var mentionsData = ParseJson<List<object>>(dict, "mentions") ?? new List<object>();
            var attachmentsData = ParseJson<List<object>>(dict, "attachments") ?? new List<object>();
            var referencesData = ParseJson<List<object>>(dict, "msg_references") ?? new List<object>();
            var contentData = ParseJson<object>(dict, "content") ?? new Dictionary<string, object>();

            return new ChannelMessage
            {
                MessageId = ToInt(dict["id"]) ?? 0,
                ClanId = ToInt(dict["clan_id"]) ?? 0,
                ChannelId = ToInt(dict["channel_id"]) ?? 0,
                SenderId = ToInt(dict["sender_id"]) ?? 0,

                Content = contentData as Dictionary<string, object>,

                Reactions = reactionsData
                    .Select(r => SafeConvert<ApiMessageReaction>(r))
                    .Where(r => r != null)
                    .ToList()!,

                Mentions = mentionsData
                    .Select(m => SafeConvert<ApiMessageMention>(m))
                    .Where(m => m != null)
                    .ToList()!,

                Attachments = attachmentsData
                    .Select(a => SafeConvert<ApiMessageAttachment>(a))
                    .Where(a => a != null)
                    .ToList()!,

                References = referencesData
                    .Select(r => SafeConvert<ApiMessageRef>(r))
                    .Where(r => r != null)
                    .ToList()!,

                CreateTimeSeconds = ToInt(dict["create_time_seconds"]) ?? 0,
                TopicId = ToInt(dict["topic_id"]) ?? 0
            };
        }

        // =========================
        // JSON PARSER (giống Python logic)
        // =========================
        private static T? ParseJson<T>(Dictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key) || dict[key] == null)
                return default;

            try
            {
                var val = dict[key];

                // Nếu là string → parse JSON
                if (val is string str)
                {
                    if (string.IsNullOrWhiteSpace(str))
                        return default;

                    return JsonSerializer.Deserialize<T>(str);
                }

                // Nếu đã là object → convert lại qua JSON
                var json = JsonSerializer.Serialize(val);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        // =========================
        // SAFE CONVERT giống model_validate
        // =========================
        private static T? SafeConvert<T>(object obj)
        {
            try
            {
                var json = JsonSerializer.Serialize(obj);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }
    }
}
