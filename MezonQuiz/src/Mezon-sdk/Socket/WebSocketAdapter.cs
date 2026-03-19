using Google.Protobuf;
using Mezon.Protobuf.Realtime;
using Mezon_sdk.Constants;
using Mezon_sdk.Utils;
using ApiUtils = Mezon_sdk.Api.Utils;
using System.Net.WebSockets;

namespace Mezon_sdk.Socket
{
	/// <summary>
	/// Interface used by Mezon socket to determine payload protocol.
	/// </summary>
	public abstract class WebSocketAdapter
	{
		protected ClientWebSocket? Socket;
		protected Task? ListenTask;

		public abstract Task ConnectAsync(string scheme, string host, string port, bool createStatus, string token);
		public abstract Task SendAsync(object message);
		public abstract Task CloseAsync();
		public abstract bool IsOpen();
	}

	/// <summary>
	/// Protobuf-based websocket adapter for binary envelope payloads.
	/// </summary>
	public class WebSocketAdapterPb : WebSocketAdapter
	{
		private static readonly Logger Logger = Logger.GetLogger("WebSocketAdapterPb");
        private readonly SemaphoreSlim _rateSemaphore;
        private readonly int _maxTokens;
        private readonly TimeSpan _period;
	    private readonly Func<string, string, string, string, IDictionary<string, object?>?, string> _utils;
        private CancellationTokenSource? _cts;
        private Task? _refillTask;

		public WebSocketAdapterPb(
			int rateLimit = RateLimit.WEBSOCKET_PB_RATE_LIMIT,
			double rateLimitPeriod = RateLimit.WEBSOCKET_PB_RATE_LIMIT_PERIOD)
		{
			_rateSemaphore = new SemaphoreSlim(rateLimit, rateLimit);
			_maxTokens = rateLimit;
			_period = TimeSpan.FromSeconds(rateLimitPeriod);
			_utils = (scheme, host, port, path, query) => ApiUtils.BuildUrl(scheme, host, port, path, query: query);
		}

        private async Task RefillTokens(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(_period, token);

                    var toRelease = _maxTokens - _rateSemaphore.CurrentCount;
                    if (toRelease > 0)
                    {
                        _rateSemaphore.Release(toRelease);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // expected khi cancel
            }
        }

		public override async Task ConnectAsync(string scheme, string host, string port, bool createStatus, string token)
		{
			var wsScheme = NormalizeWsScheme(scheme);
			var url = _utils(wsScheme, host, port, "ws", new Dictionary<string, object?>
			{
				["lang"] = "en",
				["status"] = createStatus.ToString().ToLowerInvariant(),
				["token"] = token,
				["format"] = "protobuf",
			});

			Logger.Debug($"Connecting websocket with URL: {url}");

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _refillTask = RefillTokens(_cts.Token);

			Socket?.Dispose();
			Socket = new ClientWebSocket();
			Socket.Options.AddSubProtocol("protobuf");

			await Socket.ConnectAsync(new Uri(url), CancellationToken.None);
		}

		public override async Task SendAsync(object message)
		{
			if (Socket?.State != WebSocketState.Open &&
                Socket?.State != WebSocketState.Connecting)
                return;

			byte[] payload = message switch
			{
				Envelope envelope => ProtobufHelper.EncodeProtobuf(envelope),
				IMessage protobufMessage => protobufMessage.ToByteArray(),
				byte[] raw => raw,
				_ => throw new ArgumentException($"Invalid message type: {message.GetType().FullName}")
			};

            await _rateSemaphore.WaitAsync();
			try
			{
				await Socket.SendAsync(
					new ArraySegment<byte>(payload),
					WebSocketMessageType.Binary,
					endOfMessage: true,
					cancellationToken: CancellationToken.None);
			}
			finally {}
		}

		public override async Task CloseAsync()
        {
            if (Socket == null) return;

            try
            {
                _cts?.Cancel();

                if (_refillTask != null)
                {
                    await _refillTask;
                }

                if (Socket.State == WebSocketState.Open)
                {
                    await Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by client",
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error closing websocket: {ex.Message}");
            }
            finally
            {
                Socket.Dispose();
                Socket = null;

                _cts?.Dispose();
                _cts = null;
            }
        }

        public async Task ReceiveLoopAsync(Func<byte[], Task> onMessage)
        {
            var buffer = new byte[8192];

            while (IsOpen())
            {
                using var ms = new MemoryStream();

                WebSocketReceiveResult result;

                do
                {
                    result = await Socket!.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    ms.Write(buffer, 0, result.Count);

                } while (!result.EndOfMessage);

                await onMessage(ms.ToArray());
            }
        }

		public override bool IsOpen()
		{
			return Socket != null && Socket.State == WebSocketState.Open;
		}

		private static string NormalizeWsScheme(string scheme)
		{
			var cleaned = scheme.Replace("://", string.Empty).Trim().ToLowerInvariant();
			return cleaned switch
			{
				"wss" => "wss",
				"https" => "wss",
				"http" => "ws",
				_ => "ws",
			};
		}
	}
}
