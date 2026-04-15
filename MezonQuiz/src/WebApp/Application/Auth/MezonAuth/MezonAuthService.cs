using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebApp.Application.AuditLog.Dtos;
using WebApp.Application.Auth.Login;
using WebApp.Application.Auth.Login.Dtos;
using WebApp.Application.Auth.MezonAuth.Dtos;
using WebApp.Data;
using WebApp.Domain.Entites;
using DomainUser = WebApp.Domain.Entites.User;

namespace WebApp.Application.Auth.MezonAuth
{
    public class MezonAuthService : IMezonAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly ILogger<MezonAuthService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly TimeSpan OAuthStateTtl = TimeSpan.FromMinutes(5);
        private const string OAuthStateCacheKeyPrefix = "mezon_oauth_state:";

        public MezonAuthService(
            AppDbContext dbContext,
            ITokenService tokenService,
            ILogger<MezonAuthService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<MezonCallbackResult> GetAuthorizeUrlAsync()
        {
            var clientId = _configuration["MezonOAuth2:ClientId"];
            var authorizeUrl = _configuration["MezonOAuth2:AuthorizeUrl"] ?? "https://oauth2.mezon.ai/oauth2/auth";
            var redirectUri = _configuration["MezonOAuth2:RedirectUri"];
            var scope = _configuration["MezonOAuth2:Scope"] ?? "openid offline";

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            {
                _logger.LogError("Mezon OAuth is not configured correctly. Missing ClientId/RedirectUri.");
                return Task.FromResult(MezonCallbackResult.Failure(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Cấu hình OAuth của server chưa đầy đủ." }));
            }

            var state = GenerateMezonState();
            _memoryCache.Set(GetOAuthStateCacheKey(state), true, OAuthStateTtl);

            var authUrl =
                $"{authorizeUrl}?client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                "&response_type=code" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&state={Uri.EscapeDataString(state)}";

            return Task.FromResult(MezonCallbackResult.Success(new { AuthorizeUrl = authUrl }));
        }

        public async Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "Authorization code is missing.",
                    status: "failed");
                return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, "Authorization code is missing.");
            }

            if (string.IsNullOrWhiteSpace(request.State))
            {
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "OAuth state is missing.",
                    status: "failed");
                return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, "OAuth state is missing.");
            }

            var stateCacheKey = GetOAuthStateCacheKey(request.State);
            if (!_memoryCache.TryGetValue(stateCacheKey, out _))
            {
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "Invalid or expired OAuth state.",
                    status: "failed");
                return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, "Invalid or expired OAuth state.");
            }
            _memoryCache.Remove(stateCacheKey);

            var clientId = _configuration["MezonOAuth2:ClientId"];
            var clientSecret = _configuration["MezonOAuth2:ClientSecret"];
            var tokenUrl = _configuration["MezonOAuth2:TokenUrl"] ?? "https://oauth2.mezon.ai/oauth2/token";
            var userInfoUrl = _configuration["MezonOAuth2:UserInfoUrl"] ?? "https://oauth2.mezon.ai/userinfo";
            var configuredRedirectUri = _configuration["MezonOAuth2:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(configuredRedirectUri))
            {
                _logger.LogError("Mezon OAuth is not configured correctly. Missing ClientId/ClientSecret/RedirectUri.");
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "OAuth configuration is incomplete.",
                    status: "failed");
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
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: $"Token exchange failed with status {(int)tokenResponse.StatusCode}.",
                    status: "failed");
                return MezonCallbackResult.Failure((int)tokenResponse.StatusCode, new { Message = "Lỗi khi đổi token từ Mezon.", Details = errorContent });
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (tokenData.ValueKind != JsonValueKind.Object ||
                !tokenData.TryGetProperty("access_token", out var accessTokenNode) ||
                string.IsNullOrWhiteSpace(accessTokenNode.GetString()))
            {
                var tokenRaw = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Mezon token response does not contain access_token. Body: {Body}", tokenRaw);
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "Token response does not contain access_token.",
                    status: "failed");
                return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Phản hồi token từ Mezon không hợp lệ.", Details = tokenRaw });
            }

            var mezonAccessToken = accessTokenNode.GetString()!;

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mezonAccessToken);

            var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
            {
                var userInfoError = await userInfoResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Mezon userinfo failed. Status: {StatusCode}, Body: {Body}", userInfoResponse.StatusCode, userInfoError);
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: $"Userinfo request failed with status {(int)userInfoResponse.StatusCode}.",
                    status: "failed");
                return MezonCallbackResult.Failure((int)userInfoResponse.StatusCode, new { Message = "Không thể lấy thông tin người dùng từ Mezon.", Details = userInfoError });
            }

            var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (userInfo.ValueKind != JsonValueKind.Object)
            {
                var userInfoRaw = await userInfoResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Unexpected Mezon userinfo payload: {Body}", userInfoRaw);
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "Userinfo payload is invalid.",
                    status: "failed");
                return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Phản hồi userinfo từ Mezon không hợp lệ.", Details = userInfoRaw });
            }

            string? GetStringByCandidate(JsonElement source, params string[] names)
            {
                foreach (var name in names)
                {
                    if (!source.TryGetProperty(name, out var node))
                    {
                        continue;
                    }

                    if (node.ValueKind == JsonValueKind.String)
                    {
                        var value = node.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }

                    if (node.ValueKind == JsonValueKind.Number)
                    {
                        if (node.TryGetInt64(out var longValue))
                        {
                            return longValue.ToString();
                        }

                        if (node.TryGetDouble(out var doubleValue))
                        {
                            return doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                }

                return null;
            }

            // Mezon C# SDK maps session user identity from "user_id".
            var mezonUserId = GetStringByCandidate(userInfo, "user_id", "mezon_user_id", "sub", "id")
                              ?? GetStringByCandidate(tokenData, "user_id", "mezon_user_id", "sub", "id");
            var username = GetStringByCandidate(userInfo, "username", "preferred_username", "name");
            var email = GetStringByCandidate(userInfo, "email");
            var displayName = GetStringByCandidate(userInfo, "display_name", "name");
            var avatarUrl = GetStringByCandidate(userInfo, "avatar", "picture");

            if (string.IsNullOrWhiteSpace(mezonUserId) && string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Mezon userinfo does not contain enough identity fields. Payload: {Payload}", userInfo.ToString());
                await WriteMezonAuditAsync(
                    action: "mezon.login.failed",
                    userId: null,
                    title: "Mezon Login Failed",
                    description: "Mezon userinfo does not contain enough identity fields.",
                    status: "failed");
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
                    CreatedAt = now
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

            var accessTokenResult = _tokenService.CreateAccessToken(user);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = HashToken(refreshTokenValue);

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

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

            await WriteMezonAuditAsync(
                action: "mezon.login.success",
                userId: user.Id,
                title: "Mezon Login Success",
                description: $"User '{user.Username}' logged in via Mezon OAuth.",
                status: "success");

            return MezonCallbackResult.Success(new AuthResponseDto
            {
                Token = accessTokenResult.Token,
                RefreshToken = refreshTokenValue,
                ExpiresIn = accessTokenResult.ExpiresIn,
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

        private static string GetOAuthStateCacheKey(string state) => $"{OAuthStateCacheKeyPrefix}{state}";

        private async Task WriteMezonAuditAsync(string action, Guid? userId, string title, string description, string status)
        {
            try
            {
                _dbContext.AuditLogs.Add(new WebApp.Domain.Entites.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    User = userId.HasValue ? await _dbContext.Users.FindAsync(userId.Value) : null,
                    Action = action,
                    ResourceType = "auth",
                    ResourceId = userId,
                    Details = new AuditDetailsDto
                    {
                        Title = title,
                        Description = description,
                        Status = status,
                    },
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.UtcNow,
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write Mezon auth audit log.");
            }
        }

        private string GenerateMezonState()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var state = new char[11];

            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[11];
                rng.GetBytes(buffer);

                for (int i = 0; i < state.Length; i++)
                {
                    state[i] = chars[buffer[i] % chars.Length];
                }
            }

            return new string(state);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
