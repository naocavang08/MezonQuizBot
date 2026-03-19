namespace Mezon_sdk
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.Json;
	using Mezon_sdk.Models;

	/// <summary>
	/// Represents an authenticated user session with JWT token management.
	/// </summary>
	public class Session
	{
		public string Token { get; private set; }
		public string? RefreshToken { get; private set; }
		public string UserId { get; }
		public string ApiUrl { get; }
		public string IdToken { get; }
		public string WsUrl { get; }
		public long CreatedAt { get; }
		public long? ExpiresAt { get; private set; }
		public long? RefreshExpiresAt { get; private set; }
		public Dictionary<string, object?> Vars { get; private set; }

		public Session(ApiSession apiSession)
		{
			Token = apiSession.Token ?? string.Empty;
			RefreshToken = apiSession.RefreshToken;
			UserId = apiSession.UserId?.ToString() ?? string.Empty;
			ApiUrl = apiSession.ApiUrl ?? string.Empty;
			IdToken = apiSession.IdToken ?? string.Empty;
			WsUrl = RestoreWsUrl(apiSession.WsUrl ?? string.Empty);
			CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			Vars = new Dictionary<string, object?>();

			Update(Token, RefreshToken);
		}

		public static string RestoreWsUrl(string wsUrl)
		{
			// Keep Python intent: default to secure ws if protocol is missing.
			if (!wsUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) &&
				!wsUrl.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
			{
				return $"wss://{wsUrl}";
			}

			return wsUrl;
		}

		public bool IsExpired(long currentTime)
		{
			if (!ExpiresAt.HasValue)
			{
				return true;
			}

			return (ExpiresAt.Value - currentTime) < 0;
		}

		public bool IsRefreshExpired(long currentTime)
		{
			if (!RefreshExpiresAt.HasValue)
			{
				return true;
			}

			return (RefreshExpiresAt.Value - currentTime) < 0;
		}

		public void Update(string token, string? refreshToken = null)
		{
			var payload = DecodeJwtPayload(token);

			if (!payload.TryGetValue("exp", out var expElement) ||
				expElement.ValueKind != JsonValueKind.Number ||
				!expElement.TryGetInt64(out var exp))
			{
				throw new ArgumentException("Invalid JWT token: missing exp claim.", nameof(token));
			}

			ExpiresAt = exp;
			Token = token;
			Vars = ExtractVars(payload);

			if (!string.IsNullOrWhiteSpace(refreshToken))
			{
				var refreshPayload = DecodeJwtPayload(refreshToken);
				if (!refreshPayload.TryGetValue("exp", out var refreshExpElement) ||
					refreshExpElement.ValueKind != JsonValueKind.Number ||
					!refreshExpElement.TryGetInt64(out var refreshExp))
				{
					throw new ArgumentException("Invalid refresh JWT token: missing exp claim.", nameof(refreshToken));
				}

				RefreshExpiresAt = refreshExp;
				RefreshToken = refreshToken;
			}
		}

		public static Session Restore(Dictionary<string, object?> session)
		{
			var json = JsonSerializer.Serialize(session);
			var apiSession = JsonSerializer.Deserialize<ApiSession>(json)
				?? throw new ArgumentException("Invalid session payload.", nameof(session));

			return new Session(apiSession);
		}

		private static Dictionary<string, JsonElement> DecodeJwtPayload(string jwt)
		{
			if (string.IsNullOrWhiteSpace(jwt))
			{
				throw new ArgumentException("JWT token is required.", nameof(jwt));
			}

			var parts = jwt.Split('.');
			if (parts.Length < 2)
			{
				throw new ArgumentException("Invalid JWT token format.", nameof(jwt));
			}

			var payloadBytes = Base64UrlDecode(parts[1]);
			var payloadJson = Encoding.UTF8.GetString(payloadBytes);

			var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
			return payload ?? throw new ArgumentException("Invalid JWT payload.", nameof(jwt));
		}

		private static byte[] Base64UrlDecode(string input)
		{
			var base64 = input.Replace('-', '+').Replace('_', '/');
			switch (base64.Length % 4)
			{
				case 2:
					base64 += "==";
					break;
				case 3:
					base64 += "=";
					break;
			}

			return Convert.FromBase64String(base64);
		}

		private static Dictionary<string, object?> ExtractVars(Dictionary<string, JsonElement> payload)
		{
			if (!payload.TryGetValue("vrs", out var varsElement))
			{
				return new Dictionary<string, object?>();
			}

			if (varsElement.ValueKind == JsonValueKind.Object)
			{
				var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(varsElement.GetRawText());
				return parsed ?? new Dictionary<string, object?>();
			}

			return new Dictionary<string, object?>();
		}
	}
}
