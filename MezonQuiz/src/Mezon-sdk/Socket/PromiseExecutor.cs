using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mezon_sdk.Socket
{
	/// <summary>
	/// Promise executor for handling async request/response pattern.
	/// </summary>
	public class PromiseExecutor : IDisposable
	{
		private readonly TaskCompletionSource<object?> _tcs;
        private readonly object _lock = new();
		private Timer? _timeoutHandle;
        private bool _isDisposed;

		public PromiseExecutor()
		{
			_tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public Task<object?> Future => _tcs.Task;

		/// <summary>
		/// Resolve the future with a result.
		/// </summary>
		public void Resolve(object? result)
		{
			lock (_lock)
            {
                if (_isDisposed || _tcs.Task.IsCompleted) return;
                CancelTimeout();
                _tcs.TrySetResult(result);
            }
		}

		/// <summary>
		/// Reject the future with an error.
		/// </summary>
		public void Reject(object? error)
		{
            lock (_lock)
            {
                if (_isDisposed || _tcs.Task.IsCompleted) return;
                CancelTimeout();

                var exception = error switch
                {
                    Exception ex => ex,
                    null => new Exception("Promise rejected"),
                    _ => new Exception(error.ToString()),
                };

                _tcs.TrySetException(exception);
            }
		}

		/// <summary>
		/// Set a timeout that will call the callback after delaySeconds.
		/// </summary>
		public void SetTimeout(double delaySeconds, Action callback)
        {
            lock (_lock)
            {
                if (_isDisposed || _tcs.Task.IsCompleted) return;
                CancelTimeout();

                _timeoutHandle = new Timer(
                    _ => {
                        lock (_lock)
                        {
                            if (!_tcs.Task.IsCompleted) callback();
                        }
                    },
                    null,
                    TimeSpan.FromSeconds(delaySeconds),
                    Timeout.InfiniteTimeSpan);
            }
        }

		/// <summary>
		/// Cancel the executor and cleanup resources.
		/// </summary>
		public void Cancel()
		{
			lock (_lock)
            {
                CancelTimeout();
                _tcs.TrySetCanceled();
            }
		}

		private void CancelTimeout()
        {
            _timeoutHandle?.Dispose();
            _timeoutHandle = null;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _isDisposed = true;
                CancelTimeout();
                _tcs.TrySetCanceled(); 
            }
        }
	}
}
