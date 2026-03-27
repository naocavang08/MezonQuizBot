using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mezon_sdk.Managers;
using Mezon_sdk.Models;
using Mezon_sdk.Socket;

using Pb = Mezon.Protobuf;
using Rt = Mezon.Protobuf.Realtime;
using Mezon_sdk.Structures;

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

        public ConcurrentDictionary<long, Clan> Clans { get; set; } = new();
        public ConcurrentDictionary<long, TextChannel> Channels { get; set; } = new();
        public ConcurrentDictionary<long, User> Users { get; set; } = new();

        public DefaultSocket SocketManager { get; private set; }
        public EventManager EventManager { get; private set; }

        // NOTE: Placeholder for REST api managers, which should be implemented similarly to Python's MezonApi and SessionManager via HttpClient.
        // public SessionManager SessionManager { get; private set; }
        // public ChannelManager ChannelManager { get; private set; }

        // --- C# Standard Events mapped from WebSocket payload ---
        public event Func<Mezon.Protobuf.ChannelMessage, Task>? OnChannelMessage;
        // public event Func<Rt.ChannelCreatedEvent, Task>? OnChannelCreated;
        // public event Func<Rt.ChannelUpdatedEvent, Task>? OnChannelUpdated;
        // public event Func<Rt.ChannelDeletedEvent, Task>? OnChannelDeleted;

        // Custom specific events mapped from protobuf payloads
        // public event Func<Rt.UserChannelRemoved, Task>? OnUserChannelRemoved;
        // public event Func<Rt.UserClanRemoved, Task>? OnUserClanRemoved;
        // public event Func<Rt.UserChannelAdded, Task>? OnUserChannelAdded;
        
        private readonly ILogger<MezonClient>? _logger;
        private bool _isHardDisconnect;

        /// <summary>
        /// Initialize the MezonClient.
        /// </summary>
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
            _logger = logger;

            string scheme = useSsl ? "https" : "http";
            LoginUrl = $"{scheme}://{host}:{port}";

            EventManager = new EventManager();
            SocketManager = new DefaultSocket(host, port, useSsl, eventManager: EventManager);

            RegisterInternalEventBindings();
            
            _logger?.LogInformation($"MezonClient initialized for client_id: {clientId}");
        }

        /// <summary>
        /// Wire internal pipeline: Subscribe socket payload events, handle internal caching (Auto-binding like Python),
        /// then trigger the public C# standardized events.
        /// </summary>
        private void RegisterInternalEventBindings()
        {
            EventManager.On("channel_message", new Func<object, Task>(async (payload) =>
            {
                if (payload is Mezon.Protobuf.ChannelMessage msg)
                {
                    await HandleChannelMessageDefaultAsync(msg);
                    if (OnChannelMessage != null) 
                        await OnChannelMessage.Invoke(msg);
                }
            }));

            // EventManager.On("channel_created", async (payload) =>
            // {
            //     if (payload is Rt.ChannelCreatedEvent evt)
            //     {
            //         await HandleChannelCreatedDefaultAsync(evt);
            //         if (OnChannelCreated != null)
            //             await OnChannelCreated.Invoke(evt);
            //     }
            // });

            // EventManager.On("channel_updated", async (payload) =>
            // {
            //     if (payload is Rt.ChannelUpdatedEvent evt)
            //     {
            //         await HandleChannelUpdatedDefaultAsync(evt);
            //         if (OnChannelUpdated != null)
            //             await OnChannelUpdated.Invoke(evt);
            //     }
            // });

            // ... More bindings mapped from DefaultSocket payload events to standard Events
            // These would exactly mimic the `@auto_bind` python architecture.
        }

        #region Internal Cache Handlers (Equivalent to @auto_bind callbacks in Python)
        
        private Task HandleChannelMessageDefaultAsync(Mezon.Protobuf.ChannelMessage message)
        {
            // _init_channel_message_cache pattern
            // TODO: Resolve Users, Update local message cache
            return Task.CompletedTask;
        }

        /*
        private Task HandleChannelCreatedDefaultAsync(Rt.ChannelCreatedEvent message)
        {
            // _update_cache_channel pattern
            return Task.CompletedTask;
        }

        private async Task HandleChannelUpdatedDefaultAsync(Rt.ChannelUpdatedEvent message)
        {
            // Join newly activated threads + Cache refresh
            if (message.ChannelType == 2 && message.Status == 1) // Using 2 for standard Enum placeholder on Thread Type
            {
                await SocketManager.JoinChatAsync(message.ClanId, message.ChannelId, message.ChannelType, false);
            }
            // _update_cache_channel logic
        }
        */
        
        #endregion

        /// <summary>
        /// Authenticate and initialize the client connection.
        /// </summary>
        public async Task LoginAsync(bool enableAutoReconnect = true)
        {
            _isHardDisconnect = false;

            // TODO: HTTP REST Call to get Token Session using MezonApi.
            // Example: var session = await ApiClient.GetSessionAsync(ClientId, ApiKey);
            // Example: await SocketManager.ConnectAsync(session, createStatus: true);

            // TODO: ZkProof and Mmn Initialization goes here

            if (enableAutoReconnect)
            {
                SetupReconnectHandlers();
            }
        }

        private void SetupReconnectHandlers()
        {
            // Using delegates mapped from DefaultSocket's action pipelines
            SocketManager.OnDisconnect = new Action(() => {
                _logger?.LogWarning("Socket disconnected. Attempting to auto-reconnect...");
                if (!_isHardDisconnect)
                {
                    _ = RetryConnectionAsync();
                }
            });

            SocketManager.OnError = new Action<Exception>((ex) => {
                _logger?.LogError($"Socket encountered an error/exception: {ex.Message}");
                if (!_isHardDisconnect)
                {
                    _ = RetryConnectionAsync();
                }
            });
        }

        private async Task RetryConnectionAsync()
        {
            // Exponential Backoff mechanism
            int maxRetries = 10;
            int delay = 5000;

            for (int i = 1; i <= maxRetries; i++)
            {
                if (_isHardDisconnect) return;

                try
                {
                    _logger?.LogInformation($"Reconnecting (attempt {i}/{maxRetries})...");
                    
                    // Trigger custom initialize Managers flows here
                    // await InitializeManagersAsync();
                    
                    _logger?.LogInformation("Reconnected successfully!");
                    return;
                }
                catch (Exception ex)
                {
                    if (_isHardDisconnect) return;

                    _logger?.LogWarning($"Reconnect attempt {i} failed: {ex.Message}");
                    if (i < maxRetries)
                    {
                        await Task.Delay(delay);
                        delay = Math.Min(delay * 2, 60000);
                    }
                }
            }
            _logger?.LogError($"Reconnection failed permanently after {maxRetries} attempts...");
        }

        /// <summary>
        /// Gracefully disconnect from socket preventing auto-reconnects.
        /// </summary>
        public async Task DisconnectAsync()
        {
            _isHardDisconnect = true;
            await SocketManager.CloseAsync();
            _logger?.LogInformation("Client deliberately disconnected.");
        }
    }
}
