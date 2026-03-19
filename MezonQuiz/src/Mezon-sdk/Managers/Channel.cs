namespace Mezon_sdk.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Mezon_sdk.Constants;
    using Mezon_sdk.Models;

    public class ChannelManager
    {
        private readonly MezonApi _apiClient;
        private readonly SocketManager _socketManager;
        private readonly SessionManager _sessionManager;
        
        // Dictionary mapping user_id to channel_id
        private Dictionary<long, long>? _allDmChannels;

        public ChannelManager(
            MezonApi apiClient, 
            SocketManager socketManager, 
            SessionManager sessionManager)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _socketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public async Task InitAllDmChannelsAsync(string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return;
            }
            
            var channelsResponse = await _apiClient.ListChannelsAsync(sessionToken, (int)ChannelType.ChannelTypeDm);

            if (channelsResponse == null || channelsResponse.Channeldesc == null)
            {
                return;
            }

            var dmMapping = new Dictionary<long, long>();
            foreach (var channel in channelsResponse.Channeldesc)
            {
                var userIds = channel.UserIds;
                var channelType = channel.Type;
                var channelId = channel.ChannelId;

                if (userIds != null && userIds.Any() && channelType == (int)ChannelType.ChannelTypeDm)
                {
                    if (int.TryParse(userIds[0].ToString(), out var parsedUserId))
                    {
                        dmMapping[parsedUserId] = channelId ?? 0;
                    }
                }
            }

            _allDmChannels = dmMapping;
        }

        public Dictionary<long, long>? GetAllDmChannels()
        {
            return _allDmChannels;
        }

        public async Task<ApiChannelDescription> CreateDmChannelAsync(int userId)
        {
            var session = _sessionManager.GetSession();
            if (session == null || string.IsNullOrEmpty(session.Token))
            {
                throw new InvalidOperationException("Session is not valid.");
            }

            var request = new ApiCreateChannelDescRequest
            {
                ClanId = 0,
                ChannelId = 0,
                CategoryId = 0,
                Type = (int)ChannelType.ChannelTypeDm,
                UserIds = new List<int> { userId },
                ChannelPrivate = 1
            };

            var channelDmDesc = await _apiClient.CreateChannelAsync(session.Token, request);
            
            if (channelDmDesc != null)
            {
                // Dynamic invoke placeholder for SocketManager Socket interaction
                var socket = _socketManager.GetSocket();
                if (socket != null)
                {
                    var method = socket.GetType().GetMethod("JoinChatAsync");
                    if (method != null)
                    {
                        var task = (Task)method.Invoke(socket, new object[] {
                            channelDmDesc.ClanId ?? 0,
                            channelDmDesc.ChannelId ?? 0,
                            channelDmDesc.Type ?? 0,
                            false
                        })!;
                        await task;
                    }
                }
            }

            return channelDmDesc!;
        }
    }
}
