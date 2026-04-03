using Mezon.Protobuf;
using Mezon_sdk.Constants;
using Mezon_sdk.Managers;
using Mezon_sdk.Messages;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;

namespace Mezon_sdk.Structures
{
    public class Clan
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long WelcomeChannelId { get; set; }
        
        // Client property typed as dynamic as MezonClient is not defined yet
        public dynamic? Client { get; set; } 
        public int ClientId { get; set; }

        public MezonApi ApiClient { get; set; }
        public SocketManager SocketManager { get; set; }
        public string SessionToken { get; set; }
        public MessageDbService Service { get; set; }

        private bool _channelsLoaded = false;

        public CacheManager<long, TextChannel> Channels { get; set; }

        private static readonly Logger Logger = new Logger("Clan");

        public Clan(
            long clanId, 
            string clanName, 
            long welcomeChannelId, 
            dynamic client, 
            MezonApi apiClient, 
            SocketManager socketManager, 
            string sessionToken, 
            MessageDbService service)
        {
            Id = clanId;
            Name = clanName;
            WelcomeChannelId = welcomeChannelId;
            Client = client;
            
            if (Client != null)
            {
                try { ClientId = Client.ClientId; } catch { ClientId = 0; }
            }

            ApiClient = apiClient;
            SocketManager = socketManager;
            SessionToken = sessionToken;
            Service = service;

            Channels = new CacheManager<long, TextChannel>(ChannelFetcherAsync);
        }

        private async Task<TextChannel> ChannelFetcherAsync(long channelId)
        {
            if (Client?.Channels != null)
            {
                var fetchMethod = Client.Channels.GetType().GetMethod("FetchAsync");
                if (fetchMethod != null)
                {
                    var channelTaskTask = fetchMethod.Invoke(Client.Channels, new object[] { channelId });
                    return await (dynamic)channelTaskTask;
                }
            }
            return null!;
        }

        public async Task LoadChannelsAsync()
        {
            if (_channelsLoaded) return;

            var channelsResponse = await ApiClient.ListChannelsAsync(
                token: SessionToken,
                channelType: (int)ChannelType.ChannelTypeChannel,
                clanId: checked((int)Id)
            );

            var validChannels = channelsResponse?.Channeldesc?
                .Where(c => c.ChannelId.HasValue)
                .ToList() ?? new List<ApiChannelDescription>();

            foreach (var channel in validChannels)
            {
                var channelObj = new TextChannel(
                    initChannelData: channel,
                    clan: this,
                    socketManager: SocketManager,
                    service: Service
                );
                
                if (channel.ChannelId.HasValue && long.TryParse(channel.ChannelId.Value.ToString(), out long cid))
                {
                    Channels.Set(cid, channelObj);
                    if (Client?.Channels != null)
                    {
                        var setMethod = Client.Channels.GetType().GetMethod("Set");
                        if (setMethod != null)
                        {
                            setMethod.Invoke(Client.Channels, new object[] { cid, channelObj });
                        }
                    }
                }
            }

            _channelsLoaded = true;
        }

        public async Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
            int channelId = 0,
            int? channelType = null,
            int limit = 500,
            int state = 0,
            string? cursor = null)
        {
            if (channelType == null)
            {
                channelType = (int)ChannelType.ChannelTypeGmeetVoice;
            }

            if (limit <= 0 || limit > 500)
            {
                Logger.Error("0 < limit <= 500");
                throw new ArgumentException("0 < limit <= 500");
            }

            var result = await ApiClient.ListChannelVoiceUsersAsync(
                token: SessionToken,
                clanId: checked((int)Id),
                channelId: channelId,
                channelType: channelType.Value,
                limit: limit,
                state: state,
                cursor: cursor ?? ""
            );
            return result;
        }

        public async Task<bool> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
        {
            var data = await ApiClient.UpdateRoleAsync(
                token: SessionToken,
                roleId: roleId,
                request: request
            );
            return data != null;
        }

        public async Task<ApiRoleListEventResponse> ListRolesAsync(
            int limit = 0,
            int state = 0,
            string cursor = "")
        {
            return await ApiClient.ListRolesAsync(
                token: SessionToken,
                clanId: checked((int)Id),
                limit: limit,
                state: state,
                cursor: cursor
            );
        }

        public override string ToString()
        {
            return $"<Clan id={Id} name={Name}>";
        }
    }
}