using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Mezon.Protobuf;
using Mezon_sdk.Models;
using xUTest.TestUtils;

namespace xUTest.Tests
{
    public class MezonApiTests
    {
        [Fact]
        public async Task AuthenticateAsync_SendsBasicAuthJsonBodyAndParsesProtoResponse()
        {
            var handler = new FakeHttpMessageHandler((request, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(TestProtobufFactory.CreateSessionProto(
                        token: CreateJwt(1000),
                        refreshToken: CreateJwt(2000)).ToByteArray())
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                return Task.FromResult(response);
            });
            var api = CreateApi(handler);

            var result = await api.AuthenticateAsync("user", "pass", new ApiAuthenticateRequest
            {
                Account = new ApiAccountApp
                {
                    Appid = "web"
                }
            });

            Assert.NotNull(handler.LastRequest);
            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://api.test/v2/apps/authenticate/token", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("Basic", handler.LastRequest.Headers.Authorization!.Scheme);
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass")), handler.LastRequest.Headers.Authorization.Parameter);
            Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
            Assert.Contains("application/x-protobuf", handler.LastRequest.Headers.Accept.ToString(), StringComparison.Ordinal);
            Assert.Equal(123, result.UserId);
            Assert.Equal("https://api.test", result.ApiUrl);
        }

        [Fact]
        public async Task ListClansAsync_SendsBearerHeaderAndParsesClanList()
        {
            ListClanDescRequest? captured = null;
            var handler = new FakeHttpMessageHandler(async (request, _) =>
            {
                captured = await ReadProtoAsync<ListClanDescRequest>(request);
                return CreateProtoResponse(TestProtobufFactory.CreateClanDescListProto());
            });
            var api = CreateApi(handler);

            var result = await api.ListClansAsync("token-1", limit: 10, state: 2, cursor: "abc");

            Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal("token-1", handler.LastRequest.Headers.Authorization.Parameter);
            Assert.Equal("https://api.test/mezon.api.Mezon/ListClanDescs", handler.LastRequest.RequestUri!.ToString());
            Assert.NotNull(captured);
            Assert.Equal(10, captured!.Limit);
            Assert.Equal(2, captured.State);
            Assert.Equal("abc", captured.Cursor);
            Assert.Single(result.Clandesc!);
            Assert.Equal(99, result.Clandesc![0].ClanId);
            Assert.Equal("quiz-clan", result.Clandesc[0].ClanName);
        }

        [Fact]
        public async Task ListChannelsAsync_SerializesRequestParameters()
        {
            ListChannelDescsRequest? captured = null;
            var handler = new FakeHttpMessageHandler(async (request, _) =>
            {
                captured = await ReadProtoAsync<ListChannelDescsRequest>(request);
                return CreateProtoResponse(TestProtobufFactory.CreateChannelDescListProto());
            });
            var api = CreateApi(handler);

            var result = await api.ListChannelsAsync("token-2", clanId: 7, channelType: 3, limit: 15, state: 1, cursor: "cur", isMobile: true);

            Assert.NotNull(captured);
            Assert.Equal(7, captured!.ClanId);
            Assert.Equal(3, captured.ChannelType);
            Assert.Equal(15, captured.Limit);
            Assert.Equal(1, captured.State);
            Assert.Equal("cur", captured.Cursor);
            Assert.True(captured.IsMobile);
            Assert.Single(result.Channeldesc!);
            Assert.Equal("general", result.Channeldesc![0].ChannelLabel);
            Assert.Equal("cursor-1", result.Cursor);
        }

        [Fact]
        public async Task CreateChannelAsync_MapsApiRequestToProtoRequest()
        {
            CreateChannelDescRequest? captured = null;
            var handler = new FakeHttpMessageHandler(async (request, _) =>
            {
                captured = await ReadProtoAsync<CreateChannelDescRequest>(request);
                return CreateProtoResponse(TestProtobufFactory.CreateChannelDescriptionProto());
            });
            var api = CreateApi(handler);

            var result = await api.CreateChannelAsync("token-3", new ApiCreateChannelDescRequest
            {
                ClanId = 5,
                ChannelId = 12,
                ChannelLabel = "bot-room",
                ChannelPrivate = 1,
                ParentId = 100,
                CategoryId = 200,
                Type = 4,
                UserIds = new List<int> { 9, 10 }
            });

            Assert.NotNull(captured);
            Assert.Equal(5, captured!.ClanId);
            Assert.Equal(12, captured.ChannelId);
            Assert.Equal("bot-room", captured.ChannelLabel);
            Assert.Equal(1, captured.ChannelPrivate);
            Assert.Equal(100, captured.ParentId);
            Assert.Equal(200, captured.CategoryId);
            Assert.Equal(4, captured.Type);
            Assert.Equal(new long[] { 9, 10 }, captured.UserIds);
            Assert.Equal(22, result.ChannelId);
            Assert.Equal("bot-room", result.ChannelLabel);
        }

        [Fact]
        public async Task QuickMenuApis_ParseResponsesAndPreserveRequestFields()
        {
            var responses = new Queue<IMessage>(new IMessage[]
            {
                TestProtobufFactory.CreateQuickMenuAccessProto(),
                TestProtobufFactory.CreateQuickMenuAccessProto(),
                TestProtobufFactory.CreateQuickMenuAccessListProto()
            });
            var requests = new List<HttpRequestMessage>();
            var handler = new FakeHttpMessageHandler((request, _) =>
            {
                requests.Add(request);
                return Task.FromResult(CreateProtoResponse(responses.Dequeue()));
            });
            var api = CreateApi(handler);

            var added = await api.AddQuickMenuAccessAsync("token", 11, 12, 13, "start", "red", "quiz", 14, 15);
            var deleted = await api.DeleteQuickMenuAccessAsync("token", id: 14, clanId: 12, botId: 15, channelId: 11, menuName: "quiz", background: "red", actionMsg: "start");
            var listed = await api.ListQuickMenuAccessAsync("token", botId: 15, channelId: 11, menuType: 13);

            Assert.Equal("https://api.test/mezon.api.Mezon/AddQuickMenuAccess", requests[0].RequestUri!.ToString());
            Assert.Equal("https://api.test/mezon.api.Mezon/DeleteQuickMenuAccess", requests[1].RequestUri!.ToString());
            Assert.Equal("https://api.test/mezon.api.Mezon/ListQuickMenuAccess", requests[2].RequestUri!.ToString());
            Assert.Equal(5, added.Id);
            Assert.Equal(5, deleted.Id);
            Assert.Single(listed.ListMenus!);
            Assert.Equal("Quick Quiz", listed.ListMenus![0].MenuName);
        }

        [Fact]
        public async Task PlayMediaAsync_UsesFixedEndpointAndJsonBearerRequest()
        {
            var handler = new FakeHttpMessageHandler((request, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("ok"))
                };
                return Task.FromResult(response);
            });
            var api = CreateApi(handler);

            var bytes = await api.PlayMediaAsync("media-token", new { url = "https://media.test/file.mp3" });

            Assert.NotNull(handler.LastRequest);
            Assert.Equal("https://stn.mezon.ai/api/playmedia", handler.LastRequest!.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
            Assert.Equal("media-token", handler.LastRequest.Headers.Authorization.Parameter);
            Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
            Assert.Equal("ok", Encoding.UTF8.GetString(bytes));
        }

        private static MezonApi CreateApi(FakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new MezonApi(1, "api-key", "https://api.test", 5000, httpClient);
        }

        private static HttpResponseMessage CreateProtoResponse(IMessage message)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(message.ToByteArray())
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            return response;
        }

        private static async Task<T> ReadProtoAsync<T>(HttpRequestMessage request) where T : IMessage<T>, new()
        {
            var bytes = await request.Content!.ReadAsByteArrayAsync();
            var message = new T();
            message.MergeFrom(bytes);
            return message;
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
