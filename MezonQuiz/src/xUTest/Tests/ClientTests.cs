using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mezon.Protobuf;
using Mezon_sdk;
using Mezon_sdk.Managers;
using Mezon_sdk.Messages;
using Mezon_sdk.Models;
using Mezon_sdk.Structures;

using Rt = Mezon.Protobuf.Realtime;
using PbChannelMessage = Mezon.Protobuf.ChannelMessage;
using SdkSession = Mezon_sdk.Session;
using SdkUser = Mezon_sdk.Structures.User;

namespace xUTest.Tests
{
    public class ClientTests
    {
        [Fact]
        public void Constructor_UsesDefaultConfigurationValues()
        {
            var client = new MezonClient("client-id", "api-key");

            Assert.Equal("client-id", client.ClientId);
            Assert.Equal("api-key", client.ApiKey);
            Assert.Equal(MezonClient.DefaultTimeoutMs, client.TimeoutMs);
            Assert.Equal(MezonClient.DefaultMmnApi, client.MmnApiUrl);
            Assert.Equal(MezonClient.DefaultZkApi, client.ZkApiUrl);
            Assert.Equal($"https://{MezonClient.DefaultHost}:{MezonClient.DefaultPort}/", client.LoginUrl);
            Assert.NotNull(client.EventManager);
            Assert.NotNull(client.MessageDb);
            Assert.NotNull(client.Clans);
            Assert.NotNull(client.Channels);
            Assert.NotNull(client.Users);
            Assert.Null(client.SocketManager);
        }

        [Fact]
        public void Constructor_WithCustomConfiguration_BuildsExpectedLoginUrl()
        {
            var client = new MezonClient(
                "client-id",
                "api-key",
                host: "localhost",
                port: "8080",
                useSsl: false,
                timeoutMs: 1234,
                mmnApiUrl: "https://mmn.test/",
                zkApiUrl: "https://zk.test/");

            Assert.Equal("http://localhost:8080/", client.LoginUrl);
            Assert.Equal(1234, client.TimeoutMs);
            Assert.Equal("https://mmn.test/", client.MmnApiUrl);
            Assert.Equal("https://zk.test/", client.ZkApiUrl);
            Assert.False(client.UseSsl);
        }

        [Fact]
        public async Task ChannelMessageEvent_WhenEmitted_InvokesOnChannelMessageHandler()
        {
            var client = new MezonClient("client-id", "api-key");
            var message = new PbChannelMessage
            {
                ChannelId = 42,
                Content = "hello"
            };

            PbChannelMessage? received = null;
            client.OnChannelMessage += msg =>
            {
                received = msg;
                return Task.CompletedTask;
            };

            await client.EventManager.EmitAsync("channel_message", message);
            await Task.Delay(100);

            Assert.NotNull(received);
            Assert.Same(message, received);
        }

        [Fact]
        public async Task ChannelMessageEvent_WhenUserMessageEmitted_PreservesSenderAndContent()
        {
            var client = new MezonClient("client-id", "api-key");
            var message = new PbChannelMessage
            {
                SenderId = 1843962578301095936,
                ChannelId = 556677,
                Content = "{\"t\":\"/join ABC123\"}"
            };

            PbChannelMessage? received = null;
            client.OnChannelMessage += msg =>
            {
                received = msg;
                return Task.CompletedTask;
            };

            await client.EventManager.EmitAsync("channel_message", message);
            await Task.Delay(100);

            Assert.NotNull(received);
            Assert.Equal(message.SenderId, received!.SenderId);
            Assert.Equal(message.ChannelId, received.ChannelId);
            Assert.Equal(message.Content, received.Content);
        }

        [Fact]
        public async Task OnChannelMessageEvent_WhenRegistered_InvokesHandler()
        {
            var client = new MezonClient("client-id", "api-key");
            var message = new PbChannelMessage
            {
                SenderId = 111,
                ChannelId = 222,
                Content = "{\"t\":\"/join Z9Y8X7\"}"
            };

            PbChannelMessage? received = null;
            client.OnChannelMessageEvent(msg =>
            {
                received = msg;
                return Task.CompletedTask;
            });

            await client.EventManager.EmitAsync("channel_message", message);
            await Task.Delay(100);

            Assert.NotNull(received);
            Assert.Equal(111, received!.SenderId);
            Assert.Equal("{\"t\":\"/join Z9Y8X7\"}", received.Content);
        }

        [Fact]
        public async Task ChannelMessageEvent_WithNonChannelMessagePayload_DoesNotInvokeHandler()
        {
            var client = new MezonClient("client-id", "api-key");
            var invoked = false;

            client.OnChannelMessage += _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            };

            await client.EventManager.EmitAsync("channel_message", new object());
            await Task.Delay(100);

            Assert.False(invoked);
        }

        [Fact]
        public async Task GetChannelFromIdAsync_WhenClientIsNotInitialized_ThrowsInvalidOperationException()
        {
            var client = new MezonClient("123", "api-key");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetChannelFromIdAsync(99));

            Assert.Contains("Call LoginAsync first", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AddQuickMenuAccessAsync_WhenClientIsNotInitialized_ThrowsInvalidOperationException()
        {
            var client = new MezonClient("123", "api-key");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.AddQuickMenuAccessAsync(1, 2, 3, "start", "bg", "menu"));

            Assert.Contains("Call LoginAsync first", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ChannelCreatedEvent_WhenEmitted_AddsChannelToClientAndClanCache()
        {
            var client = CreateInitializedClient();
            var clan = CreateClan(client, clanId: 10, "quiz");
            client.Clans.Set(10, clan);

            var evt = new Rt.ChannelCreatedEvent
            {
                ClanId = 10,
                ChannelId = 200,
                ChannelLabel = "general",
                ChannelType = 1,
                ChannelPrivate = 0,
                CategoryId = 9,
                ParentId = 8
            };

            await client.EventManager.EmitAsync("channel_created_event", evt);

            var channel = client.Channels.Get(200);
            Assert.NotNull(channel);
            Assert.Equal(200, channel!.Id);
            Assert.Equal("general", channel.Name);
            Assert.NotNull(clan.Channels.Get(200));
        }

        [Fact]
        public async Task ChannelDeletedEvent_WhenEmitted_RemovesChannelFromClientAndClanCache()
        {
            var client = CreateInitializedClient();
            var clan = CreateClan(client, clanId: 10, "quiz");
            var channel = new TextChannel(
                new ApiChannelDescription
                {
                    ClanId = 10,
                    ChannelId = 200,
                    ChannelLabel = "general",
                    Type = 1
                },
                clan,
                client.SocketManager!,
                client.MessageDb);

            client.Clans.Set(10, clan);
            client.Channels.Set(200, channel);
            clan.Channels.Set(200, channel);

            var evt = new Rt.ChannelDeletedEvent
            {
                ClanId = 10,
                ChannelId = 200
            };

            await client.EventManager.EmitAsync("channel_deleted_event", evt);

            Assert.Null(client.Channels.Get(200));
            Assert.Null(clan.Channels.Get(200));
        }

        [Fact]
        public async Task UserClanRemovedEvent_WhenEmitted_RemovesUsersFromCache()
        {
            var client = CreateInitializedClient();
            var user1 = new SdkUser(new UserInitData { Id = 11 }, client.SocketManager!, client.ChannelManager!);
            var user2 = new SdkUser(new UserInitData { Id = 12 }, client.SocketManager!, client.ChannelManager!);
            client.Users.Set(11, user1);
            client.Users.Set(12, user2);

            var evt = new Rt.UserClanRemoved();
            evt.UserIds.Add(11);

            await client.EventManager.EmitAsync("user_clan_removed_event", evt);

            Assert.Null(client.Users.Get(11));
            Assert.NotNull(client.Users.Get(12));
        }

        [Fact]
        public async Task ChannelCreatedEvent_WhenHandlerRegistered_InvokesOnChannelCreated()
        {
            var client = CreateInitializedClient();
            var clan = CreateClan(client, clanId: 10, "quiz");
            client.Clans.Set(10, clan);

            Rt.ChannelCreatedEvent? received = null;
            client.OnChannelCreated += evt =>
            {
                received = evt;
                return Task.CompletedTask;
            };

            var payload = new Rt.ChannelCreatedEvent
            {
                ClanId = 10,
                ChannelId = 321,
                ChannelLabel = "bot-room",
                ChannelType = 1
            };

            await client.EventManager.EmitAsync("channel_created_event", payload);

            Assert.Same(payload, received);
        }

        private static MezonClient CreateInitializedClient()
        {
            var client = new MezonClient("123", "api-key");
            var apiClient = new MezonApi(123, "api-key", "https://api.test", 5000);
            var session = new SdkSession(new ApiSession
            {
                Token = CreateJwt(2000000000),
                RefreshToken = CreateJwt(2000001000),
                UserId = 123,
                ApiUrl = "https://api.test",
                WsUrl = "wss://gw.test/ws",
                IdToken = "id-token"
            });
            var socketManager = new SocketManager("gw.test", "443", true, apiClient, client.EventManager, client, client.MessageDb);
            var sessionManager = new SessionManager(apiClient, session);
            var channelManager = new ChannelManager(apiClient, socketManager, sessionManager);

            SetProperty(client, nameof(MezonClient.ApiClient), apiClient);
            SetProperty(client, nameof(MezonClient.SocketManager), socketManager);
            SetProperty(client, nameof(MezonClient.SessionManager), sessionManager);
            SetProperty(client, nameof(MezonClient.ChannelManager), channelManager);

            return client;
        }

        private static Clan CreateClan(MezonClient client, int clanId, string clanName)
        {
            return new Clan(
                clanId,
                clanName,
                0,
                client,
                client.ApiClient!,
                client.SocketManager!,
                client.SessionManager!.GetSession()!.Token,
                client.MessageDb);
        }

        private static void SetProperty<T>(object target, string propertyName, T value)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property);
            property!.SetValue(target, value);
        }

        private static string CreateJwt(long exp)
        {
            var header = Base64UrlEncode("""{"alg":"HS256","typ":"JWT"}""");
            var payload = Base64UrlEncode($$"""{"exp":{{exp}}}""");
            return $"{header}.{payload}.signature";
        }

        private static string Base64UrlEncode(string json)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

    }
}
