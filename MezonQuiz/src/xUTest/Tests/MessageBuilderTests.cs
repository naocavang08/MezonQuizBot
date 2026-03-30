using System.Text.Json;
using Mezon_sdk.Models;
using Mezon_sdk.Socket;

namespace xUTest.Tests
{
    public class MessageBuilderTests
    {
        [Fact]
        public void ChannelMessageBuilder_Build_MapsRequiredAndOptionalFields()
        {
            var mentions = new List<ApiMessageMention>
            {
                new() { UserId = 1, Username = "alice", RoleId = 2, S = 3, E = 7 }
            };
            var attachments = new List<ApiMessageAttachment>
            {
                new() { Filename = "quiz.png", Url = "https://cdn.test/quiz.png", Filetype = "image/png", Size = 10, Width = 20, Height = 30 }
            };
            var references = new List<ApiMessageRef>
            {
                new() { MessageRefId = 55, MessageSenderId = 77, MessageSenderUsername = "bob", Content = "ref", HasAttachment = true }
            };

            var message = ChannelMessageBuilder.Build(
                clanId: 10,
                channelId: 20,
                mode: 2,
                isPublic: true,
                content: new { t = "hello" },
                mentions: mentions,
                attachments: attachments,
                references: references,
                anonymousMessage: true,
                mentionEveryone: true,
                avatar: "avatar.png",
                code: 9,
                topicId: 99);

            Assert.Equal(10L, message.ClanId);
            Assert.Equal(20L, message.ChannelId);
            Assert.Equal(2, message.Mode);
            Assert.True(message.IsPublic);
            Assert.Equal("{\"t\":\"hello\"}", message.Content);
            Assert.True(message.AnonymousMessage);
            Assert.True(message.MentionEveryone);
            Assert.Equal("avatar.png", message.Avatar);
            Assert.Equal(9, message.Code);
            Assert.Equal(99L, message.TopicId);
            Assert.Single(message.Mentions);
            Assert.Equal(1L, message.Mentions[0].UserId);
            Assert.Single(message.Attachments);
            Assert.Equal("quiz.png", message.Attachments[0].Filename);
            Assert.Single(message.References);
            Assert.Equal(55L, message.References[0].MessageRefId);
        }

        [Fact]
        public void EphemeralMessageBuilder_Build_EmbedsChannelMessageAndReceivers()
        {
            var message = EphemeralMessageBuilder.Build(
                receiverIds: new List<long> { 1, 2, 3 },
                clanId: 10,
                channelId: 20,
                mode: 2,
                isPublic: false,
                content: new Dictionary<string, object> { ["t"] = "private" });

            Assert.Equal(new long[] { 1, 2, 3 }, message.ReceiverIds);
            Assert.Equal(10L, message.Message.ClanId);
            Assert.Equal(20L, message.Message.ChannelId);
            Assert.False(message.Message.IsPublic);
            Assert.Equal("{\"t\":\"private\"}", message.Message.Content);
        }

        [Fact]
        public void ChannelMessageUpdateBuilder_Build_MapsPayloadAndFlags()
        {
            var mentions = new List<ApiMessageMention> { new() { UserId = 1, Username = "alice" } };
            var attachments = new List<ApiMessageAttachment> { new() { Filename = "file.txt", Url = "https://cdn.test/file.txt" } };

            var message = ChannelMessageUpdateBuilder.Build(
                clanId: 10,
                channelId: 20,
                mode: 2,
                isPublic: true,
                messageId: 30,
                content: new { t = "updated" },
                mentions: mentions,
                attachments: attachments,
                hideEditted: true,
                topicId: 88,
                isUpdateMsgTopic: true);

            Assert.Equal(30L, message.MessageId);
            Assert.Equal("{\"t\":\"updated\"}", message.Content);
            Assert.True(message.HideEditted);
            Assert.Equal(88L, message.TopicId);
            Assert.True(message.IsUpdateMsgTopic);
            Assert.Single(message.Mentions);
            Assert.Single(message.Attachments);
        }

        [Fact]
        public void MessageReactionBuilder_Build_MapsAllFields()
        {
            var reaction = MessageReactionBuilder.Build(
                id: 1,
                clanId: 2,
                channelId: 3,
                mode: 4,
                isPublic: true,
                messageId: 5,
                emojiId: 6,
                emoji: ":thumbsup:",
                count: 7,
                messageSenderId: 8,
                actionDelete: false);

            Assert.Equal(1L, reaction.Id);
            Assert.Equal(2L, reaction.ClanId);
            Assert.Equal(3L, reaction.ChannelId);
            Assert.Equal(4, reaction.Mode);
            Assert.True(reaction.IsPublic);
            Assert.Equal(5L, reaction.MessageId);
            Assert.Equal(6L, reaction.EmojiId);
            Assert.Equal(":thumbsup:", reaction.Emoji);
            Assert.Equal(7, reaction.Count);
            Assert.Equal(8L, reaction.MessageSenderId);
            Assert.False(reaction.Action);
        }
    }
}
