using Mezon_sdk.Socket;

namespace xUTest.Tests
{
    public class PromiseExecutorTests
    {
        [Fact]
        public async Task Resolve_CompletesFutureWithResult()
        {
            using var executor = new PromiseExecutor();

            executor.Resolve("done");

            var result = await executor.Future;
            Assert.Equal("done", result);
        }

        [Fact]
        public async Task Reject_WithException_FaultsFuture()
        {
            using var executor = new PromiseExecutor();

            executor.Reject(new InvalidOperationException("boom"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.Future);
            Assert.Equal("boom", exception.Message);
        }

        [Fact]
        public async Task Reject_WithNull_UsesDefaultMessage()
        {
            using var executor = new PromiseExecutor();

            executor.Reject(null);

            var exception = await Assert.ThrowsAsync<Exception>(async () => await executor.Future);
            Assert.Equal("Promise rejected", exception.Message);
        }

        [Fact]
        public async Task SetTimeout_InvokesCallbackThatCanResolvePromise()
        {
            using var executor = new PromiseExecutor();

            executor.SetTimeout(0.05, () => executor.Resolve("timeout-resolved"));

            var result = await executor.Future.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal("timeout-resolved", result);
        }

        [Fact]
        public async Task Cancel_CancelsFuture()
        {
            using var executor = new PromiseExecutor();

            executor.Cancel();

            await Assert.ThrowsAnyAsync<TaskCanceledException>(async () => await executor.Future);
        }

        [Fact]
        public async Task Dispose_CancelsFuture()
        {
            var executor = new PromiseExecutor();

            executor.Dispose();

            await Assert.ThrowsAnyAsync<TaskCanceledException>(async () => await executor.Future);
        }
    }
}
