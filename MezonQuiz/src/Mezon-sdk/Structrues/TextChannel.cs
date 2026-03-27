using System.Text.Json;
using Mezon_sdk.Models;
using Mezon_sdk.Managers;
using Mezon_sdk.Messages;
using Mezon_sdk.Utils;

namespace Mezon_sdk.Structures
{
    public class TextChannel
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public int? ChannelType { get; set; }
        public bool IsPrivate { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long ParentId { get; set; }
        public string MeetingCode { get; set; }
        public Clan Clan { get; set; }

        public CacheManager<long, Message> Messages { get; set; }
        public SocketManager SocketManager { get; set; }
        public MessageDbService Service { get; set; }

        public TextChannel(ApiChannelDescription initChannelData, Clan clan, SocketManager socketManager, MessageDbService service)
        {
            if (long.TryParse(initChannelData.ChannelId?.ToString(), out var id)) Id = id;
            Name = initChannelData.ChannelLabel?.ToString();
            
            if (int.TryParse(initChannelData.Type?.ToString(), out var type)) ChannelType = type;

            var isPrivStr = initChannelData.ChannelPrivate?.ToString()?.ToLower();
            IsPrivate = isPrivStr == "1" || isPrivStr == "true";

            if (long.TryParse(initChannelData.CategoryId?.ToString(), out var catId)) CategoryId = catId;
            CategoryName = initChannelData.CategoryName?.ToString() ?? "";
            
            if (long.TryParse(initChannelData.ParentId?.ToString(), out var pId)) ParentId = pId;
            MeetingCode = initChannelData.MeetingCode?.ToString() ?? "";

            Clan = clan ?? throw new ArgumentNullException(nameof(clan));
            SocketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
            Service = service ?? throw new ArgumentNullException(nameof(service));

            Messages = new CacheManager<long, Message>(MessageFetcherAsync, 200);
        }

        private async Task<Message> MessageFetcherAsync(long messageId)
        {
            var messageData = await Service.GetMessageByIdAsync(messageId.ToString(), Id?.ToString() ?? "0");
            if (messageData == null)
            {
                throw new Exception($"Message {messageId} not found on channel {Id}!");
            }
            return new Message(messageData, this, SocketManager);
        }

        public async Task<ChannelMessageAck> SendAsync(
            ChannelMessageContent content,
            List<ApiMessageMention>? mentions = null,
            List<ApiMessageAttachment>? attachments = null,
            bool? mentionEveryone = null,
            bool? anonymousMessage = null,
            long? topicId = null,
            int? code = null)
        {
            return await SocketManager.WriteChatMessageAsync(
                clanId: Clan.Id,
                channelId: Id ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode(ChannelType),
                isPublic: !IsPrivate,
                content: content,
                mentions: mentions,
                attachments: attachments,
                anonymousMessage: anonymousMessage,
                mentionEveryone: mentionEveryone,
                code: code,
                topicId: topicId
            );
        }

        public async Task<object> SendEphemeralAsync(
            List<long> receiverIds,
            object content,
            long? referenceMessageId = null,
            List<ApiMessageMention>? mentions = null,
            List<ApiMessageAttachment>? attachments = null,
            bool? mentionEveryone = null,
            bool? anonymousMessage = null,
            long? topicId = null,
            int? code = null) 
        {
            // Fallback to Ephemeral type message (default 11 if not mapped properly)
            int messageCode = code ?? 11; 

            var references = new List<ApiMessageRef>();

            if (referenceMessageId.HasValue)
            {
                var messageRef = await Messages.FetchAsync(referenceMessageId.Value);
                
                // Dynamically fetch user since MezonClient / Clan.Client.Users is not strongly typed here yet
                dynamic? user = null;
                if (Clan?.Client?.Users != null)
                {
                    var fetchMethod = Clan.Client.Users.GetType().GetMethod("FetchAsync");
                    if (fetchMethod != null)
                    {
                        var userTaskTask = fetchMethod.Invoke(Clan.Client.Users, new object[] { messageRef.SenderId ?? 0 });
                        user = await (dynamic)userTaskTask;
                    }
                }

                if (user != null)
                {
                    string? cnick = user.GetType().GetProperty("ClanNick")?.GetValue(user)?.ToString();
                    string? dname = user.GetType().GetProperty("DisplayName")?.GetValue(user)?.ToString();
                    string? uname = user.GetType().GetProperty("Username")?.GetValue(user)?.ToString();
                    string? uAvatar = user.GetType().GetProperty("Avatar")?.GetValue(user)?.ToString();
                    string? cAvatar = user.GetType().GetProperty("ClanAvatar")?.GetValue(user)?.ToString();

                    references.Add(new ApiMessageRef
                    {
                        MessageRefId = (int)messageRef.Id!,
                        MessageSenderId = (int)messageRef.SenderId!,
                        MessageSenderUsername = !string.IsNullOrEmpty(cnick) ? cnick : (!string.IsNullOrEmpty(dname) ? dname : uname),
                        MessageSenderAvatar = !string.IsNullOrEmpty(cAvatar) ? cAvatar : uAvatar,
                        Content = messageRef.Content != null ? JsonSerializer.Serialize(messageRef.Content) : ""
                    });
                }
            }

            return await SocketManager.WriteEphemeralMessageAsync(
                receiverIds: receiverIds,
                clanId: Clan!.Id,
                channelId: Id ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode(ChannelType),
                isPublic: !IsPrivate,
                content: content,
                mentions: mentions,
                attachments: attachments,
                references: references,
                anonymousMessage: anonymousMessage,
                mentionEveryone: mentionEveryone,
                code: messageCode,
                topicId: topicId
            );
        }

        public override string ToString()
        {
            return $"<TextChannel id={Id} name={Name} type={ChannelType}>";
        }
    }
}