using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mezon_sdk.Constants;
using Mezon_sdk.Managers;
using Mezon_sdk.Messages;
using Mezon_sdk.Models;
using Mezon_sdk.Structures;
using Mezon_sdk.Utils;

using Rt = Mezon.Protobuf.Realtime;
using ApiUtils = Mezon_sdk.Api.Utils;
using ApiPb = Mezon.Protobuf;

namespace Mezon_sdk
{
    public class MezonClient
    {
        public const string DefaultHost = "gw.mezon.ai";
        public const string DefaultPort = "443";
        public const bool DefaultSsl = true;
        public const int DefaultTimeoutMs = 7000;
        public const string DefaultMmnApi = "https://dong.mezon.ai/mmn-api/";
        public const string DefaultZkApi = "https://dong.mezon.ai/zk-api/";

        public string ClientId { get; }
        public string ApiKey { get; }
        public string LoginUrl { get; }
        public int TimeoutMs { get; }
        public string MmnApiUrl { get; }
        public string ZkApiUrl { get; }
        public bool UseSsl { get; }

        public CacheManager<long, Clan> Clans { get; }
        public CacheManager<long, TextChannel> Channels { get; }
        public CacheManager<long, User> Users { get; }

        public EventManager EventManager { get; private set; }
        public MessageDbService MessageDb { get; }
        public MezonApi? ApiClient { get; private set; }
        public SocketManager? SocketManager { get; private set; }
        public SessionManager? SessionManager { get; private set; }
        public ChannelManager? ChannelManager { get; private set; }

        public event Func<Mezon.Protobuf.ChannelMessage, Task>? OnChannelMessage;
        public event Func<Rt.ChannelCreatedEvent, Task>? OnChannelCreated;
        public event Func<Rt.ChannelUpdatedEvent, Task>? OnChannelUpdated;
        public event Func<Rt.ChannelDeletedEvent, Task>? OnChannelDeleted;
        public event Func<Rt.UserChannelRemoved, Task>? OnUserChannelRemoved;
        public event Func<Rt.UserClanRemoved, Task>? OnUserClanRemoved;
        public event Func<Rt.UserChannelAdded, Task>? OnUserChannelAdded;
        public event Func<ApiPb.TokenSentEvent, Task>? OnTokenSend;
        public event Func<ApiPb.MessageReaction, Task>? OnMessageReaction;
        public event Func<ApiPb.GiveCoffeeEvent, Task>? OnGiveCoffee;
        public event Func<Rt.RoleEvent, Task>? OnRoleEvent;
        public event Func<ApiPb.CreateEventRequest, Task>? OnClanEventCreated;
        public event Func<Rt.MessageButtonClicked, Task>? OnMessageButtonClicked;
        public event Func<Rt.StreamingJoinedEvent, Task>? OnStreamingJoined;
        public event Func<Rt.StreamingLeavedEvent, Task>? OnStreamingLeaved;
        public event Func<Rt.DropdownBoxSelected, Task>? OnDropdownBoxSelected;
        public event Func<Rt.WebrtcSignalingFwd, Task>? OnWebrtcSignalingFwd;
        public event Func<Rt.VoiceStartedEvent, Task>? OnVoiceStarted;
        public event Func<Rt.VoiceEndedEvent, Task>? OnVoiceEnded;
        public event Func<Rt.VoiceJoinedEvent, Task>? OnVoiceJoined;
        public event Func<Rt.VoiceLeavedEvent, Task>? OnVoiceLeaved;
        public event Func<Rt.QuickMenuDataEvent, Task>? OnQuickMenu;
        public event Func<Rt.AIAgentEnabledEvent, Task>? OnAiAgentEnabled;
        public event Func<Rt.RoleAssignedEvent, Task>? OnRoleAssign;
        public event Func<Rt.Notifications, Task>? OnNotification;
        public event Func<Rt.AddClanUserEvent, Task>? OnAddClanUser;

        private readonly string _host;
        private readonly string _port;
        private readonly ILogger<MezonClient>? _logger;

        private bool _enableAutoReconnect;
        private bool _isHardDisconnect;
        private Task? _reconnectTask;

        public MezonClient(
            string clientId,
            string apiKey,
            string host = DefaultHost,
            string port = DefaultPort,
            bool useSsl = DefaultSsl,
            int timeoutMs = DefaultTimeoutMs,
            string mmnApiUrl = DefaultMmnApi,
            string zkApiUrl = DefaultZkApi,
            ILogger<MezonClient>? logger = null)
        {
            ClientId = clientId;
            ApiKey = apiKey;
            TimeoutMs = timeoutMs;
            MmnApiUrl = mmnApiUrl;
            ZkApiUrl = zkApiUrl;
            UseSsl = useSsl;
            _host = host;
            _port = port;
            _logger = logger;

            LoginUrl = ApiUtils.BuildUrl(useSsl ? "https" : "http", host, port);

            EventManager = new EventManager();
            MessageDb = new MessageDbService();
            Clans = new CacheManager<long, Clan>(GetClanFromIdAsync, maxSize: 1000);
            Channels = new CacheManager<long, TextChannel>(GetChannelFromIdAsync, maxSize: 1000);
            Users = new CacheManager<long, User>(GetUserFromIdAsync, maxSize: 1000);

            RegisterInternalEventBindings();
            _logger?.LogInformation("MezonClient initialized for client_id: {ClientId}", clientId);
        }

        public void On(string eventName, Delegate handler)
        {
            EventManager.On(eventName, handler);
        }

        public void OnChannelMessageEvent(Func<Mezon.Protobuf.ChannelMessage, Task> handler) => OnChannelMessage += handler;
        public void OnChannelCreatedEvent(Func<Rt.ChannelCreatedEvent, Task> handler) => OnChannelCreated += handler;
        public void OnChannelUpdatedEvent(Func<Rt.ChannelUpdatedEvent, Task> handler) => OnChannelUpdated += handler;
        public void OnChannelDeletedEvent(Func<Rt.ChannelDeletedEvent, Task> handler) => OnChannelDeleted += handler;
        public void OnUserChannelRemovedEvent(Func<Rt.UserChannelRemoved, Task> handler) => OnUserChannelRemoved += handler;
        public void OnUserClanRemovedEvent(Func<Rt.UserClanRemoved, Task> handler) => OnUserClanRemoved += handler;
        public void OnUserChannelAddedEvent(Func<Rt.UserChannelAdded, Task> handler) => OnUserChannelAdded += handler;
        public void OnTokenSendEvent(Func<ApiPb.TokenSentEvent, Task> handler) => OnTokenSend += handler;
        public void OnMessageReactionEvent(Func<ApiPb.MessageReaction, Task> handler) => OnMessageReaction += handler;
        public void OnGiveCoffeeEvent(Func<ApiPb.GiveCoffeeEvent, Task> handler) => OnGiveCoffee += handler;
        public void OnRoleEventHandler(Func<Rt.RoleEvent, Task> handler) => OnRoleEvent += handler;
        public void OnClanEventCreatedEvent(Func<ApiPb.CreateEventRequest, Task> handler) => OnClanEventCreated += handler;
        public void OnMessageButtonClickedEvent(Func<Rt.MessageButtonClicked, Task> handler) => OnMessageButtonClicked += handler;
        public void OnStreamingJoinedEvent(Func<Rt.StreamingJoinedEvent, Task> handler) => OnStreamingJoined += handler;
        public void OnStreamingLeavedEvent(Func<Rt.StreamingLeavedEvent, Task> handler) => OnStreamingLeaved += handler;
        public void OnDropdownBoxSelectedEvent(Func<Rt.DropdownBoxSelected, Task> handler) => OnDropdownBoxSelected += handler;
        public void OnWebrtcSignalingFwdEvent(Func<Rt.WebrtcSignalingFwd, Task> handler) => OnWebrtcSignalingFwd += handler;
        public void OnVoiceStartedEvent(Func<Rt.VoiceStartedEvent, Task> handler) => OnVoiceStarted += handler;
        public void OnVoiceEndedEvent(Func<Rt.VoiceEndedEvent, Task> handler) => OnVoiceEnded += handler;
        public void OnVoiceJoinedEvent(Func<Rt.VoiceJoinedEvent, Task> handler) => OnVoiceJoined += handler;
        public void OnVoiceLeavedEvent(Func<Rt.VoiceLeavedEvent, Task> handler) => OnVoiceLeaved += handler;
        public void OnQuickMenuEvent(Func<Rt.QuickMenuDataEvent, Task> handler) => OnQuickMenu += handler;
        public void OnAiAgentEnabledEvent(Func<Rt.AIAgentEnabledEvent, Task> handler) => OnAiAgentEnabled += handler;
        public void OnRoleAssignEvent(Func<Rt.RoleAssignedEvent, Task> handler) => OnRoleAssign += handler;
        public void OnNotificationEvent(Func<Rt.Notifications, Task> handler) => OnNotification += handler;
        public void OnAddClanUserEvent(Func<Rt.AddClanUserEvent, Task> handler) => OnAddClanUser += handler;

        public async Task<Session> GetSessionAsync()
        {
            var apiClient = new MezonApi(ParseClientId(), ApiKey, LoginUrl, TimeoutMs);
            var sessionManager = new SessionManager(apiClient);
            return await sessionManager.AuthenticateAsync(ClientId, ApiKey);
        }

        public async Task InitializeManagersAsync(Session socketSession)
        {
            var apiUrl = ApiUtils.ParseUrlComponents(socketSession.ApiUrl, UseSsl);
            var wsUrl = ApiUtils.ParseUrlComponents(socketSession.WsUrl, UseSsl);

            ApiClient = new MezonApi(
                ParseClientId(),
                ApiKey,
                ApiUtils.BuildUrl(apiUrl.Scheme, apiUrl.Hostname, apiUrl.Port),
                TimeoutMs);

            if (SocketManager == null)
            {
                SocketManager = new SocketManager(
                    host: wsUrl.Hostname,
                    port: wsUrl.Port,
                    useSsl: wsUrl.UseSsl,
                    apiClient: ApiClient,
                    eventManager: EventManager,
                    mezonClient: this,
                    service: MessageDb);
            }
            else
            {
                SocketManager.ApiClient = ApiClient;
            }

            SessionManager = new SessionManager(ApiClient, socketSession);
            ChannelManager = new ChannelManager(ApiClient, SocketManager, SessionManager);

            await SocketManager.ConnectAsync(socketSession);

            if (!string.IsNullOrWhiteSpace(socketSession.Token))
            {
                await Task.WhenAll(
                    SocketManager.ConnectSocketAsync(socketSession.Token),
                    ChannelManager.InitAllDmChannelsAsync(socketSession.Token));
            }
        }

        public async Task LoginAsync(bool enableAutoReconnect = true)
        {
            var session = await GetSessionAsync();
            await InitializeManagersAsync(session);

            _enableAutoReconnect = enableAutoReconnect;
            _isHardDisconnect = false;

            if (enableAutoReconnect)
            {
                SetupReconnectHandlers();
            }
        }

        public async Task<Clan> GetClanFromIdAsync(long clanId)
        {
            var existing = Clans.Get(clanId);
            if (existing != null)
            {
                return existing;
            }

            EnsureInitialized();
            var session = SessionManager!.GetSession()!;
            var clans = await ApiClient!.ListClansAsync(session.Token);
            var clanDesc = clans.Clandesc?.FirstOrDefault(c => (c.ClanId ?? 0L) == clanId);

            if (clanDesc == null)
            {
                throw new KeyNotFoundException($"Clan {clanId} was not found.");
            }

            var clan = CreateClan(clanDesc, session.Token);
            Clans.Set(clanId, clan);
            return clan;
        }

        public async Task<TextChannel> GetChannelFromIdAsync(long channelId)
        {
            var existing = Channels.Get(channelId);
            if (existing != null)
            {
                return existing;
            }

            EnsureInitialized();
            var session = SessionManager!.GetSession()!;
            var channelDetail = await ApiClient!.GetChannelDetailAsync(session.Token, channelId);
            var clanId = channelDetail.ClanId ?? 0L;

            var clan = Clans.Get(clanId) ?? await GetClanFromIdAsync(clanId);
            var channel = new TextChannel(channelDetail, clan, SocketManager!, MessageDb);

            Channels.Set(channelId, channel);
            clan.Channels.Set(channelId, channel);
            return channel;
        }

        public async Task<User> GetUserFromIdAsync(long userId)
        {
            var existing = Users.Get(userId);
            if (existing != null)
            {
                return existing;
            }

            EnsureInitialized();
            var dmChannel = await ChannelManager!.CreateDmChannelAsync((int)userId);
            var channelId = dmChannel.ChannelId ?? 0;
            if (channelId == 0)
            {
                throw new KeyNotFoundException($"Unable to resolve DM channel for user {userId}.");
            }

            var user = new User(
                new UserInitData
                {
                    Id = (int)userId,
                    DmChannelId = (int)channelId
                },
                SocketManager!,
                ChannelManager);

            Users.Set(userId, user);
            return user;
        }

        public async Task<ApiQuickMenuAccess> AddQuickMenuAccessAsync(
            int channelId,
            int clanId,
            int menuType,
            string actionMsg,
            string background,
            string menuName)
        {
            EnsureInitialized();
            var session = SessionManager!.GetSession()
                ?? throw new InvalidOperationException("Session is not available.");

            return await ApiClient!.AddQuickMenuAccessAsync(
                session.Token,
                channelId,
                clanId,
                menuType,
                actionMsg,
                background,
                menuName,
                (int)Helper.GenerateSnowflakeId(),
                checked((int)ParseClientId()));
        }

        public async Task<ApiQuickMenuAccess> DeleteQuickMenuAccessAsync(
            int id = 0,
            int clanId = 0,
            int botId = 0,
            int channelId = 0,
            string menuName = "",
            string background = "",
            string actionMsg = "")
        {
            EnsureInitialized();
            var session = SessionManager!.GetSession()
                ?? throw new InvalidOperationException("Session is not available.");

            return await ApiClient!.DeleteQuickMenuAccessAsync(
                session.Token,
                id,
                clanId,
                botId,
                channelId,
                menuName,
                background,
                actionMsg);
        }

        public async Task<ApiQuickMenuAccessList> ListQuickMenuAccessAsync(
            int botId = 0,
            int channelId = 0,
            int menuType = 0)
        {
            EnsureInitialized();
            var session = SessionManager!.GetSession()
                ?? throw new InvalidOperationException("Session is not available.");

            return await ApiClient!.ListQuickMenuAccessAsync(
                session.Token,
                botId,
                channelId,
                menuType);
        }

        public async Task CloseSocketAsync()
        {
            if (SocketManager != null)
            {
                await SocketManager.DisconnectAsync();
            }
        }

        public async Task DisconnectAsync()
        {
            _isHardDisconnect = true;

            if (_reconnectTask != null && !_reconnectTask.IsCompleted)
            {
                try
                {
                    await _reconnectTask;
                }
                catch
                {
                }
            }

            await CloseSocketAsync();
            _logger?.LogInformation("Client disconnected.");
        }

        private void RegisterInternalEventBindings()
        {
            BindDefaultHandler<Mezon.Protobuf.ChannelMessage>(
                Events.ChannelMessage,
                HandleChannelMessageDefaultAsync,
                async message => await InvokeEventAsync(OnChannelMessage, message));

            BindDefaultHandler<Rt.ChannelCreatedEvent>(
                Events.ChannelCreated,
                HandleChannelCreatedDefaultAsync,
                async message => await InvokeEventAsync(OnChannelCreated, message));

            BindDefaultHandler<Rt.ChannelUpdatedEvent>(
                Events.ChannelUpdated,
                HandleChannelUpdatedDefaultAsync,
                async message => await InvokeEventAsync(OnChannelUpdated, message));

            BindDefaultHandler<Rt.ChannelDeletedEvent>(
                Events.ChannelDeleted,
                HandleChannelDeletedDefaultAsync,
                async message => await InvokeEventAsync(OnChannelDeleted, message));

            BindDefaultHandler<Rt.UserChannelRemoved>(
                Events.UserChannelRemoved,
                HandleUserChannelRemovedDefaultAsync,
                async message => await InvokeEventAsync(OnUserChannelRemoved, message));

            BindDefaultHandler<Rt.UserClanRemoved>(
                Events.UserClanRemoved,
                HandleUserClanRemovedDefaultAsync,
                async message => await InvokeEventAsync(OnUserClanRemoved, message));

            BindDefaultHandler<Rt.UserChannelAdded>(
                Events.UserChannelAdded,
                HandleUserChannelAddedDefaultAsync,
                async message => await InvokeEventAsync(OnUserChannelAdded, message));

            BindDefaultHandler<ApiPb.TokenSentEvent>(
                Events.TokenSend,
                HandleTokenSendDefaultAsync,
                async message => await InvokeEventAsync(OnTokenSend, message));

            BindDefaultHandler<ApiPb.MessageReaction>(
                Events.MessageReaction,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnMessageReaction, message));

            BindDefaultHandler<ApiPb.GiveCoffeeEvent>(
                Events.GiveCoffee,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnGiveCoffee, message));

            BindDefaultHandler<Rt.RoleEvent>(
                Events.RoleEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnRoleEvent, message));

            BindDefaultHandler<ApiPb.CreateEventRequest>(
                Events.ClanEventCreated,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnClanEventCreated, message));

            BindDefaultHandler<Rt.MessageButtonClicked>(
                Events.MessageButtonClicked,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnMessageButtonClicked, message));

            BindDefaultHandler<Rt.StreamingJoinedEvent>(
                Events.StreamingJoinedEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnStreamingJoined, message));

            BindDefaultHandler<Rt.StreamingLeavedEvent>(
                Events.StreamingLeavedEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnStreamingLeaved, message));

            BindDefaultHandler<Rt.DropdownBoxSelected>(
                Events.DropdownBoxSelected,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnDropdownBoxSelected, message));

            BindDefaultHandler<Rt.WebrtcSignalingFwd>(
                Events.WebrtcSignalingFwd,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnWebrtcSignalingFwd, message));

            BindDefaultHandler<Rt.VoiceStartedEvent>(
                Events.VoiceStartedEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnVoiceStarted, message));

            BindDefaultHandler<Rt.VoiceEndedEvent>(
                Events.VoiceEndedEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnVoiceEnded, message));

            BindDefaultHandler<Rt.VoiceJoinedEvent>(
                Events.VoiceJoinedEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnVoiceJoined, message));

            BindDefaultHandler<Rt.VoiceLeavedEvent>(
                Events.VoiceLeavedEvent,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnVoiceLeaved, message));

            BindDefaultHandler<Rt.QuickMenuDataEvent>(
                Events.QuickMenu,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnQuickMenu, message));

            BindDefaultHandler<Rt.AIAgentEnabledEvent>(
                Events.AiAgentEnable,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnAiAgentEnabled, message));

            BindDefaultHandler<Rt.RoleAssignedEvent>(
                Events.RoleAssign,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnRoleAssign, message));

            BindDefaultHandler<Rt.Notifications>(
                Events.Notifications,
                _ => Task.CompletedTask,
                async message => await InvokeEventAsync(OnNotification, message));

            BindDefaultHandler<Rt.AddClanUserEvent>(
                Events.AddClanUser,
                HandleAddClanUserDefaultAsync,
                async message => await InvokeEventAsync(OnAddClanUser, message));
        }

        private void BindDefaultHandler<T>(
            string eventName,
            Func<T, Task> defaultHandler,
            Func<T, Task>? userEventHandler = null)
        {
            EventManager.On(eventName, new Func<object, Task>(async payload =>
            {
                if (payload is not T typedPayload)
                {
                    return;
                }

                await defaultHandler(typedPayload);
                if (userEventHandler != null)
                {
                    await userEventHandler(typedPayload);
                }
            }), isDefault: true);
        }

        private async Task HandleChannelMessageDefaultAsync(Mezon.Protobuf.ChannelMessage message)
        {
            var model = ChannelMessage.FromProtobuf(message);
            await MessageDb.SaveMessageAsync(model.ToDbDict());

            if (message.ChannelId != 0 && Channels.Get(message.ChannelId) == null)
            {
                TryCreateCachedChannelFromMessage(message);
            }

            var user = Users.Get(message.SenderId);
            if (user == null && SocketManager != null && ChannelManager != null)
            {
                user = new User(
                    UserInitData.FromProtobuf(message, ResolveDmChannelId(message.SenderId)),
                    SocketManager,
                    ChannelManager);
                Users.Set(message.SenderId, user);
            }
        }

        private Task HandleChannelCreatedDefaultAsync(Rt.ChannelCreatedEvent message)
        {
            UpsertChannelFromEvent(
                message.ClanId,
                message.ChannelId,
                message.ChannelLabel,
                message.ChannelType,
                message.ChannelPrivate != 0,
                message.CategoryId,
                message.ParentId);
            return Task.CompletedTask;
        }

        private async Task HandleChannelUpdatedDefaultAsync(Rt.ChannelUpdatedEvent message)
        {
            if (message.ChannelType == (int)ChannelType.ChannelTypeThread && message.Status == 1 && SocketManager != null)
            {
                await SocketManager.GetSocket().JoinChatAsync(
                    message.ClanId,
                    message.ChannelId,
                    message.ChannelType,
                    false);
            }

            UpsertChannelFromEvent(
                message.ClanId,
                message.ChannelId,
                message.ChannelLabel,
                message.ChannelType,
                message.ChannelPrivate,
                message.CategoryId,
                message.ParentId,
                message.MeetingCode);
        }

        private Task HandleChannelDeletedDefaultAsync(Rt.ChannelDeletedEvent message)
        {
            Channels.Delete(message.ChannelId);

            var clan = Clans.Get(message.ClanId);
            clan?.Channels.Delete(message.ChannelId);
            return Task.CompletedTask;
        }

        private Task HandleUserChannelRemovedDefaultAsync(Rt.UserChannelRemoved message)
        {
            foreach (var userId in message.UserIds)
            {
                if (userId == ParseClientId())
                {
                    Channels.Delete(message.ChannelId);
                    break;
                }
            }

            return Task.CompletedTask;
        }

        private Task HandleUserClanRemovedDefaultAsync(Rt.UserClanRemoved message)
        {
            foreach (var userId in message.UserIds)
            {
                Users.Delete(userId);
            }

            return Task.CompletedTask;
        }

        private async Task HandleUserChannelAddedDefaultAsync(Rt.UserChannelAdded message)
        {
            if (SocketManager == null)
            {
                return;
            }

            foreach (var user in message.Users)
            {
                if (user.UserId == ParseClientId())
                {
                    await SocketManager.GetSocket().JoinChatAsync(
                        message.ClanId,
                        message.ChannelDesc.ChannelId,
                        message.ChannelDesc.Type,
                        message.ChannelDesc.ChannelPrivate == 0);
                    break;
                }
            }
        }

        private async Task HandleTokenSendDefaultAsync(ApiPb.TokenSentEvent message)
        {
            if (message.SenderId != ParseClientId())
            {
                return;
            }

            try
            {
                var receiverId = message.ReceiverId;
                var receiver = Users.Get(receiverId) ?? await GetUserFromIdAsync(receiverId);
                var note = string.IsNullOrWhiteSpace(message.Note) ? "Transfer funds" : message.Note;
                await receiver.SendDmMessageAsync(
                    new ChannelMessageContent
                    {
                        Text = $"Funds Transferred: {message.Amount:N0} VND | {note}"
                    },
                    code: (int)TypeMessage.SendToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to process token sent default handler.");
            }
        }

        private async Task HandleAddClanUserDefaultAsync(Rt.AddClanUserEvent message)
        {
            EnsureInitialized();

            if (message.User != null && message.User.UserId == ParseClientId())
            {
                await SocketManager!.GetSocket().JoinClanChatAsync(message.ClanId);

                if (Clans.Get(message.ClanId) == null)
                {
                    var session = SessionManager!.GetSession()
                        ?? throw new InvalidOperationException("Session is not available.");

                    var clan = CreateClan(
                        new ApiClanDesc
                        {
                            ClanId = message.ClanId,
                            ClanName = string.Empty,
                            WelcomeChannelId = 0
                        },
                        session.Token);

                    Clans.Set(message.ClanId, clan);
                    await clan.LoadChannelsAsync();
                }

                return;
            }

            if (message.User == null)
            {
                return;
            }

            var user = new User(
                new UserInitData
                {
                    Id = (int)message.User.UserId,
                    Username = message.User.Username,
                    Avatar = message.User.Avatar,
                    DisplayName = message.User.DisplayName,
                    ClanNick = string.Empty,
                    ClanAvatar = string.Empty,
                    DmChannelId = 0
                },
                SocketManager!,
                ChannelManager!);

            Users.Set(message.User.UserId, user);
        }

        private void SetupReconnectHandlers()
        {
            if (SocketManager == null)
            {
                return;
            }

            var socket = SocketManager.GetSocket();
            socket.OnDisconnect = new Action(() =>
            {
                _logger?.LogWarning("Socket disconnected.");
                if (!_isHardDisconnect && _enableAutoReconnect)
                {
                    _reconnectTask = RetryConnectionAsync();
                }
            });

            socket.OnError = new Action<Exception>(ex =>
            {
                _logger?.LogError(ex, "Socket error.");
                if (!_isHardDisconnect && _enableAutoReconnect)
                {
                    _reconnectTask = RetryConnectionAsync();
                }
            });
        }

        private async Task RetryConnectionAsync(
            int maxRetries = 10,
            int initialDelaySeconds = 5,
            int maxDelaySeconds = 60)
        {
            var delaySeconds = initialDelaySeconds;

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (_isHardDisconnect)
                {
                    return;
                }

                try
                {
                    _logger?.LogInformation("Reconnecting (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                    var session = await GetSessionAsync();
                    await InitializeManagersAsync(session);
                    SetupReconnectHandlers();
                    _logger?.LogInformation("Reconnected successfully.");
                    return;
                }
                catch (Exception ex)
                {
                    if (_isHardDisconnect)
                    {
                        return;
                    }

                    _logger?.LogWarning(ex, "Reconnect attempt {Attempt} failed.", attempt);
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        delaySeconds = Math.Min(delaySeconds * 2, maxDelaySeconds);
                    }
                }
            }

            _logger?.LogError("Reconnection failed after {MaxRetries} attempts.", maxRetries);
        }

        private void EnsureInitialized()
        {
            if (ApiClient == null || SocketManager == null || SessionManager == null || ChannelManager == null)
            {
                throw new InvalidOperationException("Client has not been initialized. Call LoginAsync first.");
            }
        }

        private Clan CreateClan(ApiClanDesc clanDesc, string sessionToken)
        {
            return new Clan(
                clanId: clanDesc.ClanId ?? 0L,
                clanName: clanDesc.ClanName ?? string.Empty,
                welcomeChannelId: clanDesc.WelcomeChannelId ?? 0L,
                client: this,
                apiClient: ApiClient!,
                socketManager: SocketManager!,
                sessionToken: sessionToken,
                service: MessageDb);
        }

        private void TryCreateCachedChannelFromMessage(Mezon.Protobuf.ChannelMessage message)
        {
            if (SocketManager == null)
            {
                return;
            }

            var clan = Clans.Get(message.ClanId);
            if (clan == null)
            {
                return;
            }

            var channel = new TextChannel(
                new ApiChannelDescription
                {
                    ClanId = (int)message.ClanId,
                    ChannelId = (int)message.ChannelId,
                    ChannelLabel = message.ChannelLabel,
                    Type = message.Mode,
                    ChannelPrivate = message.IsPublic ? 0 : 1,
                    CategoryName = message.CategoryName
                },
                clan,
                SocketManager,
                MessageDb);

            Channels.Set(message.ChannelId, channel);
            clan.Channels.Set(message.ChannelId, channel);
        }

        private void UpsertChannelFromEvent(
            long clanId,
            long channelId,
            string channelLabel,
            int channelType,
            bool isPrivate,
            long categoryId,
            long parentId,
            string meetingCode = "")
        {
            if (SocketManager == null)
            {
                return;
            }

            var clan = Clans.Get(clanId);
            if (clan == null)
            {
                return;
            }

            var channel = new TextChannel(
                new ApiChannelDescription
                {
                    ClanId = (int)clanId,
                    ChannelId = (int)channelId,
                    ChannelLabel = channelLabel,
                    Type = channelType,
                    ChannelPrivate = isPrivate ? 1 : 0,
                    CategoryId = (int)categoryId,
                    ParentId = (int)parentId,
                    MeetingCode = meetingCode
                },
                clan,
                SocketManager,
                MessageDb);

            Channels.Set(channelId, channel);
            clan.Channels.Set(channelId, channel);
        }

        private int ResolveDmChannelId(long userId)
        {
            var allDmChannels = ChannelManager?.GetAllDmChannels();
            if (allDmChannels != null && allDmChannels.TryGetValue(userId, out var channelId))
            {
                return (int)channelId;
            }

            return 0;
        }

        private long ParseClientId()
        {
            if (!long.TryParse(ClientId, out var clientId))
            {
                throw new InvalidOperationException($"ClientId '{ClientId}' is not a valid Int64 number.");
            }

            return clientId;
        }

        private static Task InvokeEventAsync<T>(Func<T, Task>? handler, T message)
        {
            return handler != null ? handler.Invoke(message) : Task.CompletedTask;
        }
    }
}
