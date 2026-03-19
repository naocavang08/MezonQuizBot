namespace Mezon_sdk.Structures
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Mezon_sdk.Models;
    using Mezon_sdk.Managers;
    using Mezon_sdk.Utils;

    public class Message
    {
        public int? Id { get; set; }
        public int? SenderId { get; set; }
        public ChannelMessageContent? Content { get; set; }
        public List<ApiMessageMention>? Mentions { get; set; }
        public List<ApiMessageAttachment>? Attachments { get; set; }
        public List<ApiMessageReaction>? Reactions { get; set; }
        public List<ApiMessageRef>? References { get; set; }
        public int? TopicId { get; set; }
        public int? CreateTimeSeconds { get; set; }

        public TextChannel Channel { get; set; }
        public SocketManager SocketManager { get; set; }

        public Message(ChannelMessage messageRaw, TextChannel channel, SocketManager socketManager)
        {
            Id = messageRaw.Id;
            SenderId = messageRaw.SenderId;
            Content = messageRaw.Content != null 
                ? JsonSerializer.Deserialize<ChannelMessageContent>(JsonSerializer.Serialize(messageRaw.Content))
                : null;
            Mentions = messageRaw.Mentions;
            Attachments = messageRaw.Attachments;
            Reactions = messageRaw.Reactions;
            References = messageRaw.References;
            TopicId = messageRaw.TopicId;

            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            SocketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
        }

        public async Task<ChannelMessageAck> ReplyAsync(
            ChannelMessageContent content,
            List<ApiMessageMention>? mentions = null,
            List<ApiMessageAttachment>? attachments = null,
            bool? mentionEveryone = null,
            bool? anonymousMessage = null,
            long? topicId = null,
            int? code = null)
        {
            // Pseudo code for fetching user since Clan/Client class needs to exist
            User? user = null;
            if (Channel?.Clan?.Client?.Users != null)
            {
                var fetchMethod = Channel.Clan.Client.Users.GetType().GetMethod("FetchAsync");
                if (fetchMethod != null)
                {
                    var userTask = (Task<User>)fetchMethod.Invoke(Channel.Clan.Client.Users, new object[] { SenderId ?? 0 })!;
                    user = await userTask;
                }
            }

            if (user == null)
            {
                throw new Exception($"User {SenderId} not found in this clan {Channel?.Clan?.Id}!");
            }

            string contentStr = "";
            if (Content != null)
            {
                contentStr = JsonSerializer.Serialize(Content);
            }

            var references = new List<ApiMessageRef>
            {
                new ApiMessageRef
                {
                    MessageRefId = Id ?? 0,
                    MessageSenderId = SenderId ?? 0,
                    MessageSenderUsername = !string.IsNullOrEmpty(user.ClanNick) ? user.ClanNick : 
                                           (!string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : user.Username),
                    MessageSenderAvatar = !string.IsNullOrEmpty(user.ClanAvatar) ? user.ClanAvatar : user.Avatar,
                    Content = contentStr
                }
            };

            return await SocketManager.WriteChatMessageAsync(
                clanId: Channel?.Clan?.Id ?? 0,
                channelId: Channel?.Id ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode(Channel?.ChannelType),
                isPublic: !(Channel?.IsPrivate ?? false),
                content: content,
                mentions: mentions,
                attachments: attachments,
                references: references,
                anonymousMessage: anonymousMessage,
                mentionEveryone: mentionEveryone,
                code: code,
                topicId: topicId ?? TopicId
            );
        }

        public async Task<ChannelMessageAck> UpdateAsync(
            ChannelMessageContent content,
            List<ApiMessageMention>? mentions = null,
            List<ApiMessageAttachment>? attachments = null)
        {
            return await SocketManager.UpdateChatMessageAsync(
                clanId: Channel?.Clan?.Id ?? 0,
                channelId: Channel?.Id ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode(Channel?.ChannelType),
                isPublic: !(Channel?.IsPrivate ?? false),
                messageId: Id ?? 0,
                content: content,
                mentions: mentions,
                attachments: attachments,
                topicId: TopicId,
                isUpdateMsgTopic: TopicId.HasValue
            );
        }

        public async Task<ApiMessageReaction> ReactAsync(
            long emojiId,
            string emoji,
            int count,
            long? id = 0,
            bool actionDelete = false)
        {
            return await SocketManager.WriteMessageReactionAsync(
                id: id ?? 0,
                clanId: Channel?.Clan?.Id ?? 0,
                channelId: Channel?.Id ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode(Channel?.ChannelType),
                isPublic: !(Channel?.IsPrivate ?? false),
                messageId: Id ?? 0,
                emojiId: emojiId,
                emoji: emoji,
                count: count,
                messageSenderId: SenderId ?? 0,
                actionDelete: actionDelete
            );
        }

        public async Task<ChannelMessageAck> DeleteAsync()
        {
            return await SocketManager.RemoveChatMessageAsync(
                clanId: Channel?.Clan?.Id ?? 0,
                channelId: Channel?.Id ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode(Channel?.ChannelType),
                isPublic: !(Channel?.IsPrivate ?? false),
                messageId: Id ?? 0,
                topicId: TopicId
            );
        }

        public override string ToString()
        {
            var contentStr = Content != null ? JsonSerializer.Serialize(Content) : "";
            var contentPreview = contentStr.Length > 50 ? contentStr.Substring(0, 50) + "..." : contentStr;
            return $"<Message id={Id} sender={SenderId} content='{contentPreview}'>";
        }
    }
}