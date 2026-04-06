using System.Collections.Concurrent;
using Google.Protobuf;
using Mezon_sdk.Managers;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;
using Pb = Mezon.Protobuf;
using Rt = Mezon.Protobuf.Realtime;

namespace Mezon_sdk.Socket
{
	public class DefaultSocket
	{
		private static readonly Logger _logger = Logger.GetLogger(nameof(DefaultSocket));

		public const int DefaultHeartbeatTimeoutMs = 10000;
		public const int DefaultSendTimeoutMs = 10000;
		public const int DefaultConnectTimeoutMs = 30000;

		public string Host { get; }
		public string Port { get; }
		public bool UseSsl { get; }
		public string WebsocketScheme => UseSsl ? "wss" : "ws";
		public int SendTimeoutMs { get; }

		public EventManager EventManager { get; }
		public WebSocketAdapterPb Adapter { get; }

		public Session? Session { get; private set; }

		private readonly ConcurrentDictionary<string, PromiseExecutor> _cids = new();
		private long _nextCid = 1;

		private readonly int _heartbeatTimeoutMs;
		private Task? _heartbeatTask;
		private Task? _listenTask;
		private CancellationTokenSource? _listenCts;
		private volatile bool _intentionalClose;

		public Delegate? OnDisconnect { get; set; }
		public Delegate? OnError { get; set; }
		public Delegate? OnHeartbeatTimeout { get; set; }
		public Delegate? OnConnect { get; set; }

		public DefaultSocket(
			string host,
			string port,
			bool useSsl = false,
			WebSocketAdapterPb? adapter = null,
			int sendTimeoutMs = DefaultSendTimeoutMs,
			EventManager? eventManager = null)
		{
			Host = host;
			Port = port;
			UseSsl = useSsl;
			SendTimeoutMs = sendTimeoutMs;
			EventManager = eventManager ?? new EventManager();
			Adapter = adapter ?? new WebSocketAdapterPb();
			_heartbeatTimeoutMs = DefaultHeartbeatTimeoutMs;
		}

		public string GenerateCid()
		{
			return Interlocked.Increment(ref _nextCid).ToString();
		}

		public bool IsOpen()
		{
			return Adapter.IsOpen();
		}

		public async Task CloseAsync()
		{
			_intentionalClose = true;

			_listenCts?.Cancel();

			if (_heartbeatTask != null)
			{
				try { await _heartbeatTask; } catch { }
			}

			if (_listenTask != null)
			{
				try { await _listenTask; } catch { }
			}

			await Adapter.CloseAsync();
		}

		public async Task<Session> ConnectAsync(
			Session session,
			bool createStatus = false,
			int connectTimeoutMs = DefaultConnectTimeoutMs)
		{
			if (Adapter.IsOpen())
			{
				return Session ?? session;
			}

			if (string.IsNullOrWhiteSpace(session.Token))
			{
				throw new InvalidOperationException("Session token is required to connect socket.");
			}

			_intentionalClose = false;
			Session = session;

			using var cts = new CancellationTokenSource(connectTimeoutMs);
			try
			{
				var connectTask = Adapter.ConnectAsync(
					WebsocketScheme,
					Host,
					Port,
					createStatus,
					session.Token!);

				var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
				if (completed != connectTask)
				{
					throw new TimeoutException("The socket timed out when trying to connect.");
				}

				await connectTask;
				await StartListenAsync();
				await InvokeCallbackAsync(OnConnect, null);
				return session;
			}
			catch (OperationCanceledException)
			{
				throw new TimeoutException("The socket timed out when trying to connect.");
			}
		}

		private async Task ListenAsync(CancellationToken cancellationToken)
		{
			try
			{
				await Adapter.ReceiveLoopAsync(async bytes =>
				{
					if (cancellationToken.IsCancellationRequested)
					{
						return;
					}

					var envelope = ProtobufHelper.ParseProtobuf(bytes);
					if (!string.IsNullOrWhiteSpace(envelope.Cid))
					{
						if (_cids.TryGetValue(envelope.Cid, out var executor))
						{
							if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.Error)
							{
								executor.Reject(envelope.Error);
							}
							else
							{
								executor.Resolve(envelope);
							}
						}
						else
						{
							_logger.Debug($"No executor found for cid: {envelope.Cid}");
						}
					}
					else
					{
						_ = EmitEventFromEnvelopeAsync(envelope);
					}
				});
			}
			catch (Exception ex)
			{
				_logger.Warning($"WebSocket connection lost: {ex.Message}");
				await InvokeCallbackAsync(OnError, ex);
			}
			finally
			{
				if (!_intentionalClose)
				{
					await InvokeCallbackAsync(OnDisconnect, null);
				}
			}
		}

		private Task StartListenAsync()
		{
			_listenCts?.Cancel();
			_listenCts = new CancellationTokenSource();

			if (_heartbeatTask == null || _heartbeatTask.IsCompleted)
			{
				_heartbeatTask = Task.Run(() => PingPongAsync(_listenCts.Token));
			}

			if (_listenTask == null || _listenTask.IsCompleted)
			{
				_listenTask = Task.Run(() => ListenAsync(_listenCts.Token));
			}

			return Task.CompletedTask;
		}

		private void CleanupCid(string cid, PromiseExecutor executor)
		{
			_cids.TryRemove(cid, out _);
			executor.Dispose();
		}

		private async Task PingPongAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(_heartbeatTimeoutMs, cancellationToken);
				}
				catch (TaskCanceledException)
				{
					return;
				}

				if (!Adapter.IsOpen())
				{
					_logger.Debug("Adapter closed, stopping heartbeat");
					return;
				}

				try
				{
					var envelope = new Rt.Envelope
					{
						Ping = new Rt.Ping(),
					};
					await SendWithCidAsync(envelope, _heartbeatTimeoutMs);
				}
				catch (Exception ex)
				{
					if (Adapter.IsOpen())
					{
						_logger.Error($"Server unreachable from heartbeat: {ex.Message}");
						await InvokeCallbackAsync(OnHeartbeatTimeout, null);
						_logger.Info("Closing socket due to heartbeat timeout");
						await Adapter.CloseAsync();
					}

					return;
				}
			}
		}

		private async Task<Rt.Envelope?> SendWithCidAsync(Rt.Envelope message, int? timeoutMs = null)
		{
			if (!Adapter.IsOpen())
			{
				throw new InvalidOperationException("Socket connection has not been established yet.");
			}

			var cid = GenerateCid();
			message.Cid = cid;

			var executor = new PromiseExecutor();
			_cids[cid] = executor;

			var effectiveTimeout = timeoutMs ?? SendTimeoutMs;

			void OnTimeout()
			{
				_logger.Warning($"Timeout waiting for response with cid: {cid} (waited {effectiveTimeout}ms)");
				CleanupCid(cid, executor);
			}

			executor.SetTimeout(effectiveTimeout / 1000.0, OnTimeout);

			try
			{
				await Adapter.SendAsync(message);
				var result = await executor.Future;
				return result as Rt.Envelope;
			}
			catch (TaskCanceledException)
			{
				throw new TimeoutException($"Request with cid {cid} timed out after {effectiveTimeout}ms");
			}
			finally
			{
				CleanupCid(cid, executor);
			}
		}

		private async Task EmitEventFromEnvelopeAsync(Rt.Envelope envelope)
		{
			if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.None)
			{
				return;
			}

			var fieldName = ToSnakeCase(envelope.MessageCase.ToString());
			var payload = GetEnvelopePayload(envelope);
			_logger.Debug($"Emitting event: {fieldName}");
			await EventManager.EmitAsync(fieldName, payload!);
		}

		private async Task<Rt.Envelope?> SendEnvelopeWithFieldAsync(string fieldName, IMessage message, int? timeoutMs = null)
		{
			var envelope = new Rt.Envelope();
			switch (fieldName)
			{
				case "channel_message_send":
					envelope.ChannelMessageSend = (Rt.ChannelMessageSend)message;
					break;
				case "channel_message_update":
					envelope.ChannelMessageUpdate = (Rt.ChannelMessageUpdate)message;
					break;
				case "ephemeral_message_send":
					envelope.EphemeralMessageSend = (Rt.EphemeralMessageSend)message;
					break;
				case "message_reaction_event":
					envelope.MessageReactionEvent = (Pb.MessageReaction)message;
					break;
				default:
					throw new ArgumentException($"Unsupported envelope field: {fieldName}");
			}

			return await SendWithCidAsync(envelope, timeoutMs);
		}

		private static T HandleResponse<T>(Rt.Envelope? response, string fieldName, string errorMessage)
			where T : class, new()
		{
			if (response != null && TryGetEnvelopeField(response, fieldName, out var payload) && payload != null)
			{
				var model = ProtoUtils.FromProtobuf<T>(payload);
				if (model != null)
				{
					return model;
				}
			}

			throw new Exception(errorMessage);
		}

		public async Task<Rt.ClanJoin> JoinClanChatAsync(long clanId)
		{
			var envelope = new Rt.Envelope
			{
				ClanJoin = new Rt.ClanJoin { ClanId = clanId }
			};

			await SendWithCidAsync(envelope);
			return envelope.ClanJoin;
		}

		public async Task<Rt.ChannelJoin> JoinChatAsync(long clanId, long channelId, int channelType, bool isPublic = true)
		{
			var envelope = new Rt.Envelope
			{
				ChannelJoin = new Rt.ChannelJoin
				{
					ClanId = clanId,
					ChannelId = channelId,
					ChannelType = channelType,
					IsPublic = isPublic,
				}
			};

			await SendWithCidAsync(envelope);
			return envelope.ChannelJoin;
		}

		public async Task<ChannelMessageAck> WriteChatMessageAsync(
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
			var channelMessageSend = ChannelMessageBuilder.Build(
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

			var response = await SendEnvelopeWithFieldAsync("channel_message_send", channelMessageSend);
			return HandleResponse<ChannelMessageAck>(
				response,
				"channel_message_ack",
				"Server did not return a channel_message_send acknowledgement.");
		}

		public async Task<ChannelMessageAck> UpdateChatMessageAsync(
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
			var channelMessageUpdate = ChannelMessageUpdateBuilder.Build(
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

			var response = await SendEnvelopeWithFieldAsync("channel_message_update", channelMessageUpdate);
			return HandleResponse<ChannelMessageAck>(
				response,
				"channel_message_ack",
				"Server did not return a channel_message_update acknowledgement.");
		}

		public async Task<ApiMessageReaction> WriteMessageReactionAsync(
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
			var messageReaction = MessageReactionBuilder.Build(
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

			var response = await SendEnvelopeWithFieldAsync("message_reaction_event", messageReaction);
			return HandleResponse<ApiMessageReaction>(
				response,
				"message_reaction_event",
				"Server did not return a message_reaction_event acknowledgement.");
		}

		public async Task<ChannelMessageAck> WriteEphemeralMessageAsync(
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
			var ephemeralMessageSend = EphemeralMessageBuilder.Build(
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

			var response = await SendEnvelopeWithFieldAsync("ephemeral_message_send", ephemeralMessageSend);
			return HandleResponse<ChannelMessageAck>(
				response,
				"channel_message_ack",
				"Server did not return an ephemeral_message_send acknowledgement.");
		}

		public async Task LeaveChatAsync(long clanId, long channelId, int channelType, bool isPublic)
		{
			var envelope = new Rt.Envelope
			{
				ChannelLeave = new Rt.ChannelLeave
				{
					ClanId = clanId,
					ChannelId = channelId,
					ChannelType = channelType,
					IsPublic = isPublic,
				}
			};

			await SendWithCidAsync(envelope);
		}

		public async Task<ChannelMessageAck> RemoveChatMessageAsync(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			long? topicId = null)
		{
			var envelope = new Rt.Envelope
			{
				ChannelMessageRemove = new Rt.ChannelMessageRemove
				{
					ClanId = clanId,
					ChannelId = channelId,
					Mode = mode,
					IsPublic = isPublic,
					MessageId = messageId,
				}
			};

			if (topicId.HasValue && topicId.Value != 0)
			{
				envelope.ChannelMessageRemove.TopicId = topicId.Value;
			}

			var response = await SendWithCidAsync(envelope);
			return HandleResponse<ChannelMessageAck>(
				response,
				"channel_message_ack",
				"Server did not return a channel_message_remove acknowledgement.");
		}

		public async Task<Dictionary<string, object>?> WriteMessageTypingAsync(long clanId, long channelId, int mode, bool isPublic)
		{
			var envelope = new Rt.Envelope
			{
				MessageTypingEvent = new Rt.MessageTypingEvent
				{
					ClanId = clanId,
					ChannelId = channelId,
					Mode = mode,
					IsPublic = isPublic,
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "message_typing_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		public async Task<Dictionary<string, object>?> WriteLastSeenMessageAsync(
			long clanId,
			long channelId,
			int mode,
			long messageId,
			long timestampSeconds)
		{
			var envelope = new Rt.Envelope
			{
				LastSeenMessageEvent = new Rt.LastSeenMessageEvent
				{
					ClanId = clanId,
					ChannelId = channelId,
					Mode = mode,
					MessageId = messageId,
					TimestampSeconds = checked((uint)timestampSeconds),
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "last_seen_message_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		public async Task<Dictionary<string, object>?> WriteLastPinMessageAsync(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			long timestampSeconds,
			int operation)
		{
			var envelope = new Rt.Envelope
			{
				LastPinMessageEvent = new Rt.LastPinMessageEvent
				{
					ClanId = clanId,
					ChannelId = channelId,
					Mode = mode,
					IsPublic = isPublic,
					MessageId = messageId,
					TimestampSeconds = checked((uint)timestampSeconds),
					Operation = operation,
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "last_pin_message_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		public async Task<Dictionary<string, object>?> WriteCustomStatusAsync(long clanId, string status)
		{
			var envelope = new Rt.Envelope
			{
				CustomStatusEvent = new Rt.CustomStatusEvent
				{
					ClanId = clanId,
					Status = status,
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "custom_status_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		public async Task<Dictionary<string, object>?> WriteVoiceJoinedAsync(
			string id,
			long clanId,
			string clanName,
			long voiceChannelId,
			string voiceChannelLabel,
			string participant,
			string lastScreenshot = "")
		{
			var envelope = new Rt.Envelope
			{
				VoiceJoinedEvent = new Rt.VoiceJoinedEvent
				{
					Id = id,
					ClanId = clanId,
					ClanName = clanName,
					VoiceChannelId = voiceChannelId,
					VoiceChannelLabel = voiceChannelLabel,
					Participant = participant,
					LastScreenshot = lastScreenshot,
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "voice_joined_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		public async Task<Dictionary<string, object>?> WriteVoiceLeavedAsync(
			string id,
			long clanId,
			long voiceChannelId,
			long voiceUserId)
		{
			var envelope = new Rt.Envelope
			{
				VoiceLeavedEvent = new Rt.VoiceLeavedEvent
				{
					Id = id,
					ClanId = clanId,
					VoiceChannelId = voiceChannelId,
					VoiceUserId = voiceUserId,
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "voice_leaved_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		public async Task<Dictionary<string, object>?> CheckDuplicateClanNameAsync(string clanName)
		{
			var envelope = new Rt.Envelope
			{
				CheckNameExistedEvent = new Rt.CheckNameExistedEvent
				{
					Name = clanName,
				}
			};

			var response = await SendWithCidAsync(envelope);
			return TryGetEnvelopeField(response, "check_name_existed_event", out var payload)
				? ProtoMessageToDictionary(payload)
				: null;
		}

		// TODO: Match Python placeholders; implement when API contracts are finalized.
		public Task<Dictionary<string, object>?> ListClanEmojiByClanIdAsync(string clanId) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> ListChannelByUserIdAsync() => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> HashtagDmListAsync(List<string> userIds, int limit) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> ListClanStickersByClanIdAsync(string clanId) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> GetNotificationChannelSettingAsync(long channelId) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> GetNotificationCategorySettingAsync(long categoryId) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> GetNotificationClanSettingAsync(long clanId) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task<Dictionary<string, object>?> GetNotificationReactMessageAsync(long channelId) => Task.FromResult<Dictionary<string, object>?>(null);
		public Task UpdateStatusAsync(string? status = null) => Task.CompletedTask;

		private static bool TryGetEnvelopeField(Rt.Envelope? envelope, string fieldName, out IMessage? payload)
		{
			payload = null;
			if (envelope == null)
			{
				return false;
			}

			switch (fieldName)
			{
				case "channel_message_ack":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.ChannelMessageAck)
					{
						payload = envelope.ChannelMessageAck;
						return true;
					}
					break;
				case "message_reaction_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.MessageReactionEvent)
					{
						payload = envelope.MessageReactionEvent;
						return true;
					}
					break;
				case "channel_message":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.ChannelMessage)
					{
						payload = envelope.ChannelMessage;
						return true;
					}
					break;
				case "message_typing_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.MessageTypingEvent)
					{
						payload = envelope.MessageTypingEvent;
						return true;
					}
					break;
				case "last_seen_message_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.LastSeenMessageEvent)
					{
						payload = envelope.LastSeenMessageEvent;
						return true;
					}
					break;
				case "last_pin_message_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.LastPinMessageEvent)
					{
						payload = envelope.LastPinMessageEvent;
						return true;
					}
					break;
				case "custom_status_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.CustomStatusEvent)
					{
						payload = envelope.CustomStatusEvent;
						return true;
					}
					break;
				case "voice_joined_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.VoiceJoinedEvent)
					{
						payload = envelope.VoiceJoinedEvent;
						return true;
					}
					break;
				case "voice_leaved_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.VoiceLeavedEvent)
					{
						payload = envelope.VoiceLeavedEvent;
						return true;
					}
					break;
				case "check_name_existed_event":
					if (envelope.MessageCase == Rt.Envelope.MessageOneofCase.CheckNameExistedEvent)
					{
						payload = envelope.CheckNameExistedEvent;
						return true;
					}
					break;
			}

			return false;
		}

		private static IMessage? GetEnvelopePayload(Rt.Envelope envelope)
		{
			return envelope.MessageCase switch
			{
				Rt.Envelope.MessageOneofCase.Channel => envelope.Channel,
				Rt.Envelope.MessageOneofCase.ClanJoin => envelope.ClanJoin,
				Rt.Envelope.MessageOneofCase.ChannelJoin => envelope.ChannelJoin,
				Rt.Envelope.MessageOneofCase.ChannelLeave => envelope.ChannelLeave,
				Rt.Envelope.MessageOneofCase.ChannelMessage => envelope.ChannelMessage,
				Rt.Envelope.MessageOneofCase.ChannelMessageAck => envelope.ChannelMessageAck,
				Rt.Envelope.MessageOneofCase.ChannelMessageSend => envelope.ChannelMessageSend,
				Rt.Envelope.MessageOneofCase.ChannelMessageUpdate => envelope.ChannelMessageUpdate,
				Rt.Envelope.MessageOneofCase.ChannelMessageRemove => envelope.ChannelMessageRemove,
				Rt.Envelope.MessageOneofCase.MessageReactionEvent => envelope.MessageReactionEvent,
				Rt.Envelope.MessageOneofCase.MessageButtonClicked => envelope.MessageButtonClicked,
				Rt.Envelope.MessageOneofCase.Error => envelope.Error,
				Rt.Envelope.MessageOneofCase.Ping => envelope.Ping,
				Rt.Envelope.MessageOneofCase.Pong => envelope.Pong,
				Rt.Envelope.MessageOneofCase.MessageTypingEvent => envelope.MessageTypingEvent,
				Rt.Envelope.MessageOneofCase.LastSeenMessageEvent => envelope.LastSeenMessageEvent,
				Rt.Envelope.MessageOneofCase.LastPinMessageEvent => envelope.LastPinMessageEvent,
				Rt.Envelope.MessageOneofCase.CustomStatusEvent => envelope.CustomStatusEvent,
				Rt.Envelope.MessageOneofCase.VoiceJoinedEvent => envelope.VoiceJoinedEvent,
				Rt.Envelope.MessageOneofCase.VoiceLeavedEvent => envelope.VoiceLeavedEvent,
				Rt.Envelope.MessageOneofCase.CheckNameExistedEvent => envelope.CheckNameExistedEvent,
				_ => null,
			};
		}

		private static Dictionary<string, object> ProtoMessageToDictionary(IMessage? message)
		{
			if (message == null)
			{
				return new Dictionary<string, object>();
			}

			var json = JsonFormatter.Default.Format(message);
			return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json)
				?? new Dictionary<string, object>();
		}

		private static async Task InvokeCallbackAsync(Delegate? callback, Exception? exception)
		{
			if (callback == null)
			{
				return;
			}

			try
			{
				switch (callback)
				{
					case Func<Task> f0:
						await f0();
						break;
					case Func<Exception?, Task> f1:
						await f1(exception);
						break;
					case Action a0:
						a0();
						break;
					case Action<Exception?> a1:
						a1(exception);
						break;
				}
			}
			catch (Exception ex)
			{
				_logger.Error($"Error in callback: {ex.Message}");
			}
		}

		private static string ToSnakeCase(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			var chars = new List<char>(value.Length + 8);
			for (var i = 0; i < value.Length; i++)
			{
				var c = value[i];
				if (char.IsUpper(c) && i > 0)
				{
					chars.Add('_');
				}
				chars.Add(char.ToLowerInvariant(c));
			}

			return new string(chars.ToArray());
		}
	}
}
