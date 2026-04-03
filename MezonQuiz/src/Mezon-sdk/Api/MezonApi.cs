using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Mezon.Protobuf;
using Mezon_sdk.Constants;
using Mezon_sdk.Models;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Linq;
using static Mezon.Protobuf.Friend.Types;
using Mezon_sdk.Api;

public class MezonApi
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly int _timeoutMs;

    private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan _rateDelay = TimeSpan.FromMilliseconds(1250);

    public MezonApi(long clientId, string apiKey, string baseUrl, int timeoutMs)
        : this(clientId, apiKey, baseUrl, timeoutMs, httpClient: null)
    {
    }

    public MezonApi(long clientId, string apiKey, string baseUrl, int timeoutMs, HttpClient? httpClient)
    {
        _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
        _timeoutMs = timeoutMs;

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
    }

    // ========================
    // CORE CALL API
    // ========================
    private async Task<byte[]> CallApiAsync(
        HttpMethod method,
        string urlPath,
        byte[]? body = null,
        string? bearerToken = null,
        bool isBinary = true)
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var request = new HttpRequestMessage(method, $"{_baseUrl}{urlPath}");

            if (bearerToken != null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            if (isBinary)
            {
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
            }

            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/x-protobuf");
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        finally
        {
            _ = Task.Delay(_rateDelay).ContinueWith(_ => _rateLimiter.Release());
        }
    }

    // ========================
    // GENERIC PARSE
    // ========================
    private T ParseProto<T>(byte[] data) where T : IMessage<T>, new()
    {
        var msg = new T();
        msg.MergeFrom(data);
        return msg;
    }

    // ========================
    // AUTHENTICATE
    // ========================
    public async Task<ApiSession> AuthenticateAsync(
        string username,
        string password,
        ApiAuthenticateRequest body)
    {
        var jsonBody = Utils.BuildBody(body);
        var requestBytes = Encoding.UTF8.GetBytes(jsonBody);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_baseUrl}/v2/apps/authenticate/token");

        var byteContent = new ByteArrayContent(requestBytes);
        byteContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/json");

        request.Content = byteContent;

        var authToken = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", authToken);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/x-protobuf"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsByteArrayAsync();
        return ApiSession.FromProtobuf(ParseProto<Session>(data))!;
    }

    // ========================
    // LIST CLANS
    // ========================
    public async Task<ApiClanDescList> ListClansAsync(
        string token,
        int limit = 0,
        int state = 0,
        string cursor = "")
    {
        var req = new ListClanDescRequest
        {
            Limit = limit,
            State = state,
            Cursor = cursor ?? ""
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/ListClanDescs",
            req.ToByteArray(),
            token);

        return ApiClanDescList.FromProtobuf(ParseProto<ClanDescList>(data))!;
    }

    // ========================
    // LIST CHANNELS
    // ========================
    public async Task<ApiChannelDescList> ListChannelsAsync(
        string token,
        int clanId = 0,
        int channelType = 0,
        int limit = 0,
        int state = 0,
        string cursor = "",
        bool isMobile = false)
    {
        var req = new ListChannelDescsRequest
        {
            ClanId = clanId,
            ChannelType = channelType,
            Limit = limit,
            State = state,
            Cursor = cursor ?? "",
            IsMobile = isMobile
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/ListChannelDescs",
            req.ToByteArray(),
            token);

        return ApiChannelDescList.FromProtobuf(ParseProto<ChannelDescList>(data))!;
    }

    // ========================
    // CREATE CHANNEL
    // ========================
    public async Task<ApiChannelDescription> CreateChannelAsync(
        string token,
        ApiCreateChannelDescRequest request)
    {
        var proto = new CreateChannelDescRequest
        {
            ClanId = request.ClanId ?? 0,
            ChannelId = request.ChannelId ?? 0,
            ChannelLabel = request.ChannelLabel ?? "",
            ChannelPrivate = request.ChannelPrivate ?? 0,
            ParentId = request.ParentId ?? 0,
            CategoryId = request.CategoryId ?? 0,
            Type = request.Type ?? 0
        };

        proto.UserIds.Add((request.UserIds ?? new List<int>()).Select(static id => (long)id));

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/CreateChannelDesc",
            proto.ToByteArray(),
            token);

        return ApiChannelDescription.FromProtobuf(ParseProto<ChannelDescription>(data))!;
    }

    // ========================
    // GET CHANNEL DETAIL
    // ========================
    public async Task<ApiChannelDescription> GetChannelDetailAsync(
        string token,
        long channelId)
    {
        var req = new ListChannelDetailRequest
        {
            ChannelId = channelId
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/ListChannelDetail",
            req.ToByteArray(),
            token);

        return ApiChannelDescription.FromProtobuf(ParseProto<ChannelDescription>(data))!;
    }

    // ========================
    // LIST CHANNEL VOICE USERS
    // ========================
    public async Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string token,
        int clanId = 0,
        int channelId = 0,
        int channelType = 0,
        int limit = 0,
        int state = 0,
        string cursor = "",
        Dictionary<string, object> options = null!)
    {
        var req = new ListChannelUsersRequest
        {
            ClanId = clanId,
            ChannelId = channelId,
            ChannelType = channelType,
            Limit = limit,
            State = state,
            Cursor = cursor ?? ""
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/ListChannelVoiceUsers",
            req.ToByteArray(),
            token);
        return ApiVoiceChannelUserList.FromProtobuf(ParseProto<VoiceChannelUserList>(data))!;
    }

    // ========================
    // UPDATE ROLE
    // ========================
    public async Task<byte[]> UpdateRoleAsync(
        string token,
        int roleId,
        UpdateRoleRequest request)
    {
        var protoReq = new UpdateRoleRequest
        {
            RoleId = roleId,
            Title = request.Title ?? "",
            Color = request.Color ?? "",
            RoleIcon = request.RoleIcon ?? "",
            Description = request.Description ?? "",
            DisplayOnline = request.DisplayOnline,
            AllowMention = request.AllowMention,
            ClanId = request.ClanId
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/UpdateRole",
            protoReq.ToByteArray(),
            token);

        return data; 
    }

    // ========================
    // LIST ROLES
    // ========================
    public async Task<ApiRoleListEventResponse> ListRolesAsync(
        string token,
        int clanId = 0,
        int limit = 0,
        int state = 0,
        string cursor = "")
    {
        var req = new RoleListEventRequest
        {
            ClanId = clanId,
            Limit = limit,
            State = state,
            Cursor = cursor ?? ""
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/ListRoles",
            req.ToByteArray(),
            token);

        return ApiRoleListEventResponse.FromProtobuf(ParseProto<RoleListEventResponse>(data))!;
    }

    // ========================
    // ADD QUICK MENU ACCESS
    // ========================
    public async Task<ApiQuickMenuAccess> AddQuickMenuAccessAsync(
        string token,
        int channelId,
        int clanId,
        int menuType,
        string actionMsg,
        string background,
        string menuName,
        int menuId,
        int botId)
    {
        var req = new QuickMenuAccess
        {
            Id = menuId,
            BotId = botId,
            ClanId = clanId,
            ChannelId = channelId,
            MenuName = menuName,
            Background = background,
            ActionMsg = actionMsg,
            MenuType = menuType
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/AddQuickMenuAccess",
            req.ToByteArray(),
            token);

        return ApiQuickMenuAccess.FromProtobuf(ParseProto<QuickMenuAccess>(data))!;
    }

    // ========================
    // DELETE QUICK MENU ACCESS
    // ========================
    public async Task<ApiQuickMenuAccess> DeleteQuickMenuAccessAsync(
        string token,
        int id = 0,
        int clanId = 0,
        int botId = 0,
        int channelId = 0,
        string menuName = "",
        string background = "",
        string actionMsg = "")
    {
        var req = new QuickMenuAccess
        {
            Id = id,
            BotId = botId,
            ClanId = clanId,
            ChannelId = channelId,
            MenuName = menuName,
            Background = background,
            ActionMsg = actionMsg
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/DeleteQuickMenuAccess",
            req.ToByteArray(),
            token);

        return ApiQuickMenuAccess.FromProtobuf(ParseProto<QuickMenuAccess>(data))!;
    }

    // ========================
    // LIST QUICK MENU ACCESS
    // ========================
    public async Task<ApiQuickMenuAccessList> ListQuickMenuAccessAsync(
        string token,
        int botId = 0,
        int channelId = 0,
        int menuType = 0)
    {
        var req = new ListQuickMenuAccessRequest
        {
            BotId = botId,
            ChannelId = channelId,
            MenuType = menuType
        };

        var data = await CallApiAsync(
            HttpMethod.Post,
            "/mezon.api.Mezon/ListQuickMenuAccess",
            req.ToByteArray(),
            token);

        return ApiQuickMenuAccessList.FromProtobuf(ParseProto<QuickMenuAccessList>(data))!;
    }

    // ========================
    // PLAY MEDIA
    // ========================
    public async Task<byte[]> PlayMediaAsync(
        string token,
        object body)
    {
        var jsonBody = JsonSerializer.Serialize(body);
        var requestBytes = Encoding.UTF8.GetBytes(jsonBody);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://stn.mezon.ai/api/playmedia");

        var byteContent = new ByteArrayContent(requestBytes);
        byteContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/json");

        request.Content = byteContent;
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
}
