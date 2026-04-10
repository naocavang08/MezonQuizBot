using Mezon_sdk.Constants;
using Mezon_sdk.Managers;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;

namespace Mezon_sdk.Structures
{
    public class User
    {
        public long Id { get; set; }
        public string? Avatar { get; set; }
        public long? DmChannelId { get; set; }
        public string? Username { get; set; }
        public string? ClanNick { get; set; }
        public string? ClanAvatar { get; set; }
        public string? DisplayName { get; set; }

        public ChannelManager ChannelManager { get; set; }
        public SocketManager SocketManager { get; set; }
        
        private static readonly Logger Logger = new Logger("User");

        public User(UserInitData userInitData, SocketManager socketManager, ChannelManager channelManager)
        {
            if (userInitData == null) throw new ArgumentNullException(nameof(userInitData));
            
            Id = userInitData.Id; 
            Avatar = userInitData.Avatar?.ToString();
            
            DmChannelId = userInitData.DmChannelId;

            Username = userInitData.Username?.ToString();
            ClanNick = userInitData.ClanNick?.ToString();
            ClanAvatar = userInitData.ClanAvatar?.ToString();
            DisplayName = userInitData.DisplayName?.ToString();

            SocketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
            ChannelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        }

        public async Task<ApiChannelDescription> CreateDmChannelAsync()
        {
            Logger.Debug($"Creating DM channel for user {Id}");
            return await ChannelManager.CreateDmChannelAsync(Id);
        }

        public async Task<ChannelMessageAck> SendDmMessageAsync(
            ChannelMessageContent content,
            int code = (int)TypeMessage.Chat,
            List<ApiMessageAttachment>? attachments = null)
        {
            if (DmChannelId == null || DmChannelId == 0)
            {
                var dmChannel = await CreateDmChannelAsync();
                
                if (dmChannel != null && dmChannel.ChannelId != null)
                {
                    DmChannelId = dmChannel.ChannelId;
                }
            }

            Logger.Debug($"Sending DM message to user {Id} with channel {DmChannelId}");

            return await SocketManager.WriteChatMessageAsync(
                clanId: 0,
                channelId: DmChannelId ?? 0,
                mode: Helper.ConvertChannelTypeToChannelMode((int)ChannelType.ChannelTypeDm),
                isPublic: false,
                content: content,
                code: code,
                attachments: attachments
            );
        }

        public override string ToString()
        {
            return $"<User id={Id} username={Username} display_name={DisplayName}>";
        }
    }
}