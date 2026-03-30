using Mezon_sdk.Messages;

namespace xUTest.Integration
{
    public sealed class MessageDbServiceTests : IAsyncLifetime
    {
        private MessageDbService _service = null!;

        public Task InitializeAsync()
        {
            _service = new MessageDbService();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _service.DisposeAsync();
        }

        [Fact]
        public async Task SaveMessageAsync_PersistsAndReadsBackById()
        {
            await _service.SaveMessageAsync(CreateMessage("100", "10", 1000, "hello"));

            var message = await _service.GetMessageByIdAsync("100", "10");
            var messages = await _service.GetMessagesByChannelAsync("10", limit: 1);

            Assert.NotNull(message);
            Assert.Equal(100, message!.MessageId);
            Assert.Equal(10, message.ChannelId);
            Assert.Equal(1000, message.CreateTimeSeconds);
            Assert.Single(messages);
            Assert.Equal("{\"t\":\"hello\"}", messages[0]["content"]?.ToString());
        }

        [Fact]
        public async Task SaveMessageAsync_UpdateDoesNotIncreaseCount()
        {
            await _service.SaveMessageAsync(CreateMessage("100", "10", 1000, "v1"));
            await _service.SaveMessageAsync(CreateMessage("100", "10", 1001, "v2"));

            var count = await _service.GetMessageCountAsync();
            var channelCount = await _service.GetMessageCountAsync("10");
            var message = await _service.GetMessageByIdAsync("100", "10");
            var messages = await _service.GetMessagesByChannelAsync("10", limit: 1);

            Assert.Equal(1, count);
            Assert.Equal(1, channelCount);
            Assert.NotNull(message);
            Assert.Equal(1001, message.CreateTimeSeconds);
            Assert.Single(messages);
            Assert.Equal("{\"t\":\"v2\"}", messages[0]["content"]?.ToString());
        }

        [Fact]
        public async Task GetMessagesByChannelAsync_ReturnsNewestFirstWithPaging()
        {
            await _service.SaveMessageAsync(CreateMessage("1", "10", 1000, "old"));
            await _service.SaveMessageAsync(CreateMessage("2", "10", 2000, "mid"));
            await _service.SaveMessageAsync(CreateMessage("3", "10", 3000, "new"));

            var firstPage = await _service.GetMessagesByChannelAsync("10", limit: 2, offset: 0);
            var secondPage = await _service.GetMessagesByChannelAsync("10", limit: 1, offset: 2);

            Assert.Equal(new[] { "3", "2" }, firstPage.Select(x => x["id"]!.ToString()).ToArray());
            Assert.Single(secondPage);
            Assert.Equal("1", secondPage[0]["id"]!.ToString());
        }

        [Fact]
        public async Task DeleteMessageAsync_RemovesPayloadAndUpdatesCounts()
        {
            await _service.SaveMessageAsync(CreateMessage("100", "10", 1000, "hello"));

            var deleted = await _service.DeleteMessageAsync("100", "10");
            var message = await _service.GetMessageByIdAsync("100", "10");
            var count = await _service.GetMessageCountAsync();

            Assert.True(deleted);
            Assert.Null(message);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task ClearChannelMessagesAsync_RemovesOnlyTargetChannel()
        {
            await _service.SaveMessageAsync(CreateMessage("1", "10", 1000, "a"));
            await _service.SaveMessageAsync(CreateMessage("2", "10", 2000, "b"));
            await _service.SaveMessageAsync(CreateMessage("3", "11", 3000, "c"));

            var deletedCount = await _service.ClearChannelMessagesAsync("10");
            var channel10Count = await _service.GetMessageCountAsync("10");
            var channel11Count = await _service.GetMessageCountAsync("11");
            var totalCount = await _service.GetMessageCountAsync();

            Assert.Equal(2, deletedCount);
            Assert.Equal(0, channel10Count);
            Assert.Equal(1, channel11Count);
            Assert.Equal(1, totalCount);
        }

        [Fact]
        public async Task SaveMessageAsync_IgnoresPayloadWithoutRequiredKeys()
        {
            await _service.SaveMessageAsync(new Dictionary<string, object>
            {
                ["content"] = new Dictionary<string, object> { ["t"] = "missing ids" }
            });

            Assert.Equal(0, await _service.GetMessageCountAsync());
        }

        private static Dictionary<string, object> CreateMessage(string messageId, string channelId, long createTime, string text)
        {
            return new Dictionary<string, object>
            {
                ["message_id"] = messageId,
                ["channel_id"] = channelId,
                ["clan_id"] = "1",
                ["sender_id"] = "2",
                ["topic_id"] = "0",
                ["create_time_seconds"] = createTime,
                ["content"] = new Dictionary<string, object> { ["t"] = text },
                ["mentions"] = new List<object>(),
                ["attachments"] = new List<object>(),
                ["reactions"] = new List<object>(),
                ["references"] = new List<object>()
            };
        }
    }
}
