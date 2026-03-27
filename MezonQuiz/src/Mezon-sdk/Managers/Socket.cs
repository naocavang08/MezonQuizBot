namespace Mezon_sdk.Managers
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Threading.Tasks;
	using Mezon_sdk.Messages;
	using Mezon_sdk.Models;
	using Mezon_sdk.Socket;
	using Mezon_sdk.Structures;
	using Mezon_sdk.Utils;

	public class SocketManager
	{
		private static readonly Logger _logger = Logger.GetLogger(nameof(SocketManager));

		public string Host { get; }
		public string Port { get; }
		public bool UseSsl { get; }
		public MezonApi ApiClient { get; set; }
		public EventManager EventManager { get; }

		private readonly object _mezonClient;
		private readonly MessageDbService _service;
		private readonly WebSocketAdapterPb _adapter;
		private readonly DefaultSocket _socket;

		public SocketManager(
			string host,
			string port,
			bool useSsl,
			MezonApi apiClient,
			EventManager eventManager,
			object mezonClient,
            MessageDbService service)
		{
			Host = host;
			Port = port;
			UseSsl = useSsl;
			ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			EventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
			_mezonClient = mezonClient ?? throw new ArgumentNullException(nameof(mezonClient));
            _service = service ?? throw new ArgumentNullException(nameof(service));

			_adapter = new WebSocketAdapterPb();
			_socket = new DefaultSocket(
				host: host,
				port: port,
				useSsl: useSsl,
				adapter: _adapter,
				eventManager: eventManager);
		}

		public DefaultSocket GetSocket()
		{
			return _socket;
		}

		public async Task<Session> ConnectAsync(Session apiSession)
		{
			if (_socket.IsOpen())
			{
				await _socket.CloseAsync();
			}

			return await _socket.ConnectAsync(apiSession, createStatus: true);
		}

		public Task<bool> IsConnectedAsync()
		{
			return Task.FromResult(_socket.IsOpen());
		}

		public async Task ConnectSocketAsync(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
			{
				throw new ArgumentException("Token is required.", nameof(token));
			}

			Exception? lastError = null;

			for (var attempt = 1; attempt <= 3; attempt++)
			{
				try
				{
					var clans = await ApiClient.ListClansAsync(token);
					clans.Clandesc ??= new List<ApiClanDesc>();
					clans.Clandesc.Add(new ApiClanDesc { ClanId = 0, ClanName = string.Empty });

					await JoinAllClansAsync(clans.Clandesc, token);
					return;
				}
				catch (Exception ex)
				{
					lastError = ex;
					if (attempt >= 3)
					{
						break;
					}

					var seconds = Math.Min(15.0, Math.Max(1.5, Math.Pow(2, attempt - 1)));
					_logger.Warning($"ConnectSocketAsync attempt {attempt} failed: {ex.Message}. Retrying in {seconds:0.##}s.");
					await Task.Delay(TimeSpan.FromSeconds(seconds));
				}
			}

			throw lastError ?? new Exception("Failed to connect socket after retries.");
		}

		public async Task JoinAllClansAsync(List<ApiClanDesc> clans, string token)
		{
			if (clans == null)
			{
				return;
			}

			var tasks = new List<Task>();

			foreach (var clanDesc in clans)
			{
				var clanId = clanDesc.ClanId ?? 0;
				tasks.Add(_socket.JoinClanChatAsync(clanId));

				var clan = new Clan(
					clanId: clanId,
					clanName: clanDesc.ClanName ?? string.Empty,
					welcomeChannelId: clanDesc.WelcomeChannelId ?? 0,
					client: _mezonClient,
					apiClient: ApiClient,
					socketManager: this,
					sessionToken: token,
					service: _service);

				SetClanOnClient(clanId, clan);
			}

			await Task.WhenAll(tasks);
		}

		public Task<ChannelMessageAck> WriteEphemeralMessageAsync(
			List<long> receiverIds,
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			object content,
			List<ApiMessageMention>? mentions = null,
			List<ApiMessageAttachment>? attachments = null,
			List<ApiMessageRef>? references = null,
			bool? anonymousMessage = null,
			bool? mentionEveryone = null,
			string? avatar = null,
			int? code = null,
			long? topicId = null)
		{
			return _socket.WriteEphemeralMessageAsync(
				receiverIds,
				clanId,
				channelId,
				mode,
				isPublic,
				content,
				mentions,
				attachments,
				references,
				anonymousMessage,
				mentionEveryone,
				avatar,
				code,
				topicId);
		}

		public Task<ChannelMessageAck> WriteChatMessageAsync(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			object content,
			List<ApiMessageMention>? mentions = null,
			List<ApiMessageAttachment>? attachments = null,
			List<ApiMessageRef>? references = null,
			bool? anonymousMessage = null,
			bool? mentionEveryone = null,
			string? avatar = null,
			int? code = null,
			long? topicId = null)
		{
			return _socket.WriteChatMessageAsync(
				clanId,
				channelId,
				mode,
				isPublic,
				content,
				mentions,
				attachments,
				references,
				anonymousMessage,
				mentionEveryone,
				avatar,
				code,
				topicId);
		}

		public Task<ChannelMessageAck> UpdateChatMessageAsync(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			object content,
			List<ApiMessageMention>? mentions = null,
			List<ApiMessageAttachment>? attachments = null,
			bool hideEditted = false,
			long? topicId = null,
			bool? isUpdateMsgTopic = null)
		{
			return _socket.UpdateChatMessageAsync(
				clanId,
				channelId,
				mode,
				isPublic,
				messageId,
				content,
				mentions,
				attachments,
				hideEditted,
				topicId,
				isUpdateMsgTopic);
		}

		public Task<ApiMessageReaction> WriteMessageReactionAsync(
			long id,
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			long emojiId,
			string emoji,
			int count,
			long messageSenderId,
			bool actionDelete = false)
		{
			return _socket.WriteMessageReactionAsync(
				id,
				clanId,
				channelId,
				mode,
				isPublic,
				messageId,
				emojiId,
				emoji,
				count,
				messageSenderId,
				actionDelete);
		}

		public Task<ChannelMessageAck> RemoveChatMessageAsync(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			long? topicId = null)
		{
			return _socket.RemoveChatMessageAsync(
				clanId,
				channelId,
				mode,
				isPublic,
				messageId,
				topicId);
		}

		public Task DisconnectAsync()
		{
			return _socket.CloseAsync();
		}

		private void SetClanOnClient(int clanId, Clan clan)
		{
			if (clanId <= 0)
			{
				return;
			}

			var clientType = _mezonClient.GetType();

			var clansProp = clientType.GetProperty("Clans", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
			var clansStore = clansProp?.GetValue(_mezonClient);
			if (clansStore == null)
			{
				return;
			}

			var setMethod = clansStore.GetType().GetMethod("Set", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
			if (setMethod != null)
			{
				setMethod.Invoke(clansStore, new object[] { (long)clanId, clan });
			}
		}
	}
}
