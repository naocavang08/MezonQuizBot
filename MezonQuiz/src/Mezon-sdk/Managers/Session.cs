namespace Mezon_sdk.Managers
{
	using System;
	using System.Threading.Tasks;
	using Mezon_sdk.Models;

	public class SessionManager
	{
		private readonly MezonApi _apiClient;
		private Session? _session;

		public SessionManager(MezonApi apiClient, Session? session = null)
		{
			_apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			_session = session;
		}

		public Session? GetSession()
		{
			return _session;
		}

		public async Task<Session> AuthenticateAsync(string clientId, string clientSecret)
		{
			var request = new ApiAuthenticateRequest
			{
				Account = new ApiAccountApp
				{
					Appid = clientId,
					Token = clientSecret,
				},
			};

			var apiSession = await _apiClient.AuthenticateAsync(clientId, clientSecret, request);
			_session = new Session(apiSession);
			return _session;
		}

		public Task<Session> AuthenticateAsync(int clientId, string clientSecret)
		{
			return AuthenticateAsync(clientId.ToString(), clientSecret);
		}
	}
}
