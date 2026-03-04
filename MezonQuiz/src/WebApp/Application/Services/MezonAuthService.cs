using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Area.User.Dtos;
using WebApp.Data;
using DomainUser = WebApp.Domain.Entites.User;

namespace WebApp.Application.Services
{
    public class MezonAuthService : IMezonAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly ILogger<MezonAuthService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MezonAuthService(
            AppDbContext dbContext,
            ITokenService tokenService,
            ILogger<MezonAuthService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, "Authorization code is missing.");
            }

            var clientId = _configuration["MezonOAuth2:ClientId"];
            var clientSecret = _configuration["MezonOAuth2:ClientSecret"];
            var tokenUrl = _configuration["MezonOAuth2:TokenUrl"] ?? "https://oauth2.mezon.ai/oauth2/token";
            var userInfoUrl = _configuration["MezonOAuth2:UserInfoUrl"] ?? "https://oauth2.mezon.ai/userinfo";
            var configuredRedirectUri = _configuration["MezonOAuth2:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(configuredRedirectUri))
            {
                _logger.LogError("Mezon OAuth is not configured correctly. Missing ClientId/ClientSecret/RedirectUri.");
                return MezonCallbackResult.Failure(StatusCodes.Status500InternalServerError, "Cấu hình OAuth của server chưa đầy đủ.");
            }

            var httpClient = _httpClientFactory.CreateClient();

            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", request.Code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", configuredRedirectUri)
            });

            var tokenResponse = await httpClient.PostAsync(tokenUrl, tokenRequestContent);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Mezon token exchange failed. Status: {StatusCode}, Body: {Body}", tokenResponse.StatusCode, errorContent);
                return MezonCallbackResult.Failure((int)tokenResponse.StatusCode, new { Message = "Lỗi khi đổi token từ Mezon.", Details = errorContent });
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (tokenData.ValueKind != JsonValueKind.Object ||
                !tokenData.TryGetProperty("access_token", out var accessTokenNode) ||
                string.IsNullOrWhiteSpace(accessTokenNode.GetString()))
            {
                var tokenRaw = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Mezon token response does not contain access_token. Body: {Body}", tokenRaw);
                return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Phản hồi token từ Mezon không hợp lệ.", Details = tokenRaw });
            }

            var accessToken = accessTokenNode.GetString()!;

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
            {
                var userInfoError = await userInfoResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Mezon userinfo failed. Status: {StatusCode}, Body: {Body}", userInfoResponse.StatusCode, userInfoError);
                return MezonCallbackResult.Failure((int)userInfoResponse.StatusCode, new { Message = "Không thể lấy thông tin người dùng từ Mezon.", Details = userInfoError });
            }

            var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (userInfo.ValueKind != JsonValueKind.Object)
            {
                var userInfoRaw = await userInfoResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Unexpected Mezon userinfo payload: {Body}", userInfoRaw);
                return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Phản hồi userinfo từ Mezon không hợp lệ.", Details = userInfoRaw });
            }

            string? GetStringByCandidate(JsonElement source, params string[] names)
            {
                foreach (var name in names)
                {
                    if (source.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.String)
                    {
                        return node.GetString();
                    }
                }

                return null;
            }

            var mezonUserId = GetStringByCandidate(userInfo, "mezon_user_id", "sub", "id");
            var username = GetStringByCandidate(userInfo, "username", "preferred_username", "name");
            var email = GetStringByCandidate(userInfo, "email");
            var displayName = GetStringByCandidate(userInfo, "display_name", "name");
            var avatarUrl = GetStringByCandidate(userInfo, "avatar", "picture");

            if (string.IsNullOrWhiteSpace(mezonUserId) && string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Mezon userinfo does not contain enough identity fields. Payload: {Payload}", userInfo.ToString());
                return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Không thể xác định người dùng từ Mezon." });
            }

            var now = DateTime.UtcNow;
            DomainUser? user = null;

            if (!string.IsNullOrWhiteSpace(mezonUserId))
            {
                user = await _dbContext.Users.FirstOrDefaultAsync(u => u.MezonUserId == mezonUserId);
            }

            if (user is null && !string.IsNullOrWhiteSpace(email))
            {
                user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            }

            if (user is null)
            {
                var baseUsername = !string.IsNullOrWhiteSpace(username)
                    ? username.Trim()
                    : $"mezon_{mezonUserId!.Trim()}";

                var uniqueUsername = baseUsername;
                var suffix = 1;

                while (await _dbContext.Users.AnyAsync(u => u.Username == uniqueUsername))
                {
                    uniqueUsername = $"{baseUsername}_{suffix}";
                    suffix++;
                }

                user = new DomainUser
                {
                    MezonUserId = mezonUserId,
                    Username = uniqueUsername,
                    Email = email,
                    DisplayName = displayName,
                    AvatarUrl = avatarUrl,
                    IsActive = true,
                    LastLoginAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.Users.Add(user);
            }
            else
            {
                user.MezonUserId ??= mezonUserId;
                user.Email ??= email;
                user.DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : user.DisplayName;
                user.AvatarUrl = !string.IsNullOrWhiteSpace(avatarUrl) ? avatarUrl : user.AvatarUrl;
                user.LastLoginAt = now;
                user.UpdatedAt = now;
                user.IsActive = true;
            }

            await _dbContext.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);
            var roles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Name, r.IsSystem })
                .Distinct()
                .ToListAsync();

            var roleNames = roles
                .Select(r => r.Name)
                .ToList();

            var hasSystemRole = roles.Any(r => r.IsSystem);

            var permissionNames = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
                .Join(_dbContext.Permissions, permissionId => permissionId, p => p.Id, (permissionId, p) => p.Resource + "." + p.Action)
                .Distinct()
                .ToListAsync();

            return MezonCallbackResult.Success(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.DisplayName,
                    user.AvatarUrl
                },
                RoleName = roleNames,
                PermissionName = permissionNames,
                HasSystemRole = hasSystemRole
            });
        }
    }
}