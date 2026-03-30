using System;
using System.Collections.Generic;
using System.Text;
using Mezon_sdk;
using Mezon_sdk.Models;

namespace xUTest.Tests
{
    public class SessionTests
    {
        [Fact]
        public void RestoreWsUrl_AddsSecureSchemeWhenMissing()
        {
            var result = Session.RestoreWsUrl("gateway.mezon.ai/socket");

            Assert.Equal("wss://gateway.mezon.ai/socket", result);
        }

        [Theory]
        [InlineData("wss://gateway.mezon.ai/socket")]
        [InlineData("ws://gateway.mezon.ai/socket")]
        public void RestoreWsUrl_KeepsExistingScheme(string wsUrl)
        {
            var result = Session.RestoreWsUrl(wsUrl);

            Assert.Equal(wsUrl, result);
        }

        [Fact]
        public void Constructor_ParsesTokenRefreshTokenAndVars()
        {
            var token = CreateJwt(2000000000, """{"role":"admin","enabled":true}""");
            var refreshToken = CreateJwt(2000001000);
            var apiSession = new ApiSession
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = 42,
                ApiUrl = "https://api.mezon.ai",
                IdToken = "id-token",
                WsUrl = "socket.mezon.ai"
            };

            var session = new Session(apiSession);

            Assert.Equal(token, session.Token);
            Assert.Equal(refreshToken, session.RefreshToken);
            Assert.Equal("42", session.UserId);
            Assert.Equal("wss://socket.mezon.ai", session.WsUrl);
            Assert.Equal(2000000000, session.ExpiresAt);
            Assert.Equal(2000001000, session.RefreshExpiresAt);
            Assert.Equal("admin", session.Vars["role"]);
            Assert.Equal(true, session.Vars["enabled"]);
        }

        [Theory]
        [InlineData(199, 200, true)]
        [InlineData(200, 200, false)]
        [InlineData(201, 200, false)]
        public void IsExpired_ReturnsExpectedValue(long currentTime, long expiresAt, bool expected)
        {
            var session = new Session(new ApiSession
            {
                Token = CreateJwt(expiresAt),
                RefreshToken = CreateJwt(expiresAt + 100)
            });

            var result = session.IsExpired(currentTime);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(299, 300, true)]
        [InlineData(300, 300, false)]
        [InlineData(301, 300, false)]
        public void IsRefreshExpired_ReturnsExpectedValue(long currentTime, long expiresAt, bool expected)
        {
            var session = new Session(new ApiSession
            {
                Token = CreateJwt(200),
                RefreshToken = CreateJwt(expiresAt)
            });

            var result = session.IsRefreshExpired(currentTime);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Update_WithoutRefreshToken_UpdatesAccessTokenOnly()
        {
            var originalRefresh = CreateJwt(500);
            var session = new Session(new ApiSession
            {
                Token = CreateJwt(200),
                RefreshToken = originalRefresh
            });

            session.Update(CreateJwt(300, """{"scope":"quiz"}"""));

            Assert.Equal(300, session.ExpiresAt);
            Assert.Equal(originalRefresh, session.RefreshToken);
            Assert.Equal(500, session.RefreshExpiresAt);
            Assert.Equal("quiz", session.Vars["scope"]);
        }

        [Fact]
        public void Update_WithoutExpClaim_ThrowsArgumentException()
        {
            var session = new Session(new ApiSession
            {
                Token = CreateJwt(200),
                RefreshToken = CreateJwt(500)
            });

            var exception = Assert.Throws<ArgumentException>(() => session.Update(CreateJwtWithoutExp()));

            Assert.Contains("missing exp claim", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Restore_RebuildsSessionFromDictionaryPayload()
        {
            var token = CreateJwt(700);
            var refreshToken = CreateJwt(900);
            var payload = new Dictionary<string, object?>
            {
                ["token"] = token,
                ["refresh_token"] = refreshToken,
                ["user_id"] = 314,
                ["api_url"] = "https://api.restore",
                ["id_token"] = "restore-id",
                ["ws_url"] = "restore.socket"
            };

            var session = Session.Restore(payload);

            Assert.Equal(token, session.Token);
            Assert.Equal(refreshToken, session.RefreshToken);
            Assert.Equal("314", session.UserId);
            Assert.Equal("wss://restore.socket", session.WsUrl);
        }

        private static string CreateJwt(long exp, string? varsJson = null)
        {
            var payload = varsJson is null
                ? $$"""{"exp":{{exp}}}"""
                : $$"""{"exp":{{exp}},"vrs":{{varsJson}}}""";
            return CreateTokenFromPayload(payload);
        }

        private static string CreateJwtWithoutExp()
        {
            return CreateTokenFromPayload("""{"sub":"missing-exp"}""");
        }

        private static string CreateTokenFromPayload(string payloadJson)
        {
            var header = Base64UrlEncode("""{"alg":"HS256","typ":"JWT"}""");
            var payload = Base64UrlEncode(payloadJson);
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
