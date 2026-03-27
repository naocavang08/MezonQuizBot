using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;

namespace Mezon_sdk.Messages
{
    public class MessageDbService : IAsyncDisposable
    {
        private readonly string _connectionString = "DataSource=MezonCache;Mode=Memory;Cache=Shared";
        private readonly SqliteConnection _masterConnection;
        private readonly DbContextOptions<MessageDbContext> _dbContextOptions;
        private static readonly Logger Logger = new Logger("MessageDB");

        public MessageDbService()
        {
            _masterConnection = new SqliteConnection(_connectionString);
            _masterConnection.Open();

            var builder = new DbContextOptionsBuilder<MessageDbContext>();
            builder.UseSqlite(_masterConnection);
            _dbContextOptions = builder.Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
            Logger.Debug("In-Memory SQLite Database initialized");
        }

        private MessageDbContext CreateContext () => new MessageDbContext(_dbContextOptions);

        public async Task SaveMessageAsync(Dictionary<string, object> message)
        {
            using var context = CreateContext();

            var id = message.GetValueOrDefault("message_id")?.ToString();
            var channelId = message.GetValueOrDefault("channel_id")?.ToString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(channelId)) return;
            // Kiểm tra xem message đã tồn tại chưa, nếu có thì update, nếu không thì tạo mới
            var entity = await context.Messages
                .FirstOrDefaultAsync(m => m.Id == id && m.ChannelId == channelId)
                ?? new MessageEntity { Id = id, ChannelId = channelId };

            entity.ClanId = message.GetValueOrDefault("clan_id")?.ToString();
            entity.SenderId = message.GetValueOrDefault("sender_id")?.ToString();
            entity.TopicId = message.GetValueOrDefault("topic_id")?.ToString();

            var createTime = message.GetValueOrDefault("create_time_seconds");
            entity.CreateTimeSeconds = createTime != null ? Convert.ToInt64(createTime) : null;

            // Serialize JSON
            entity.Content = SerializeJson(message, "content", new Dictionary<string, object>());
            entity.Mentions = SerializeJson(message, "mentions", new List<object>());
            entity.Attachments = SerializeJson(message, "attachments", new List<object>());
            entity.Reactions = SerializeJson(message, "reactions", new List<object>());
            entity.MsgReferences = SerializeJson(message, "references", new List<object>());

            if (context.Entry(entity).State == EntityState.Detached)
            {
                await context.Messages.AddAsync(entity);
            }

            await context.SaveChangesAsync();
            Logger.Debug($"Saved message {id}");
        }

        public async Task<ChannelMessage?> GetMessageByIdAsync(string messageId, string channelId)
        {
            using var context = CreateContext();

            var entity = await context.Messages
                .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.Id == messageId);

            if (entity != null)
            {
                var dict = MapEntityToDictionary(entity);
                return ChannelMessage.FromDictionary(dict);
            }

            return null;
        }

        public async Task<List<Dictionary<string, object>>> GetMessagesByChannelAsync(string channelId, int limit = 50, int offset = 0)
        {
            using var context = CreateContext();

            var entities = await context.Messages
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.CreateTimeSeconds)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            return entities.Select(MapEntityToDictionary).ToList();
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string channelId)
        {
            using var context = CreateContext();

            var deletedCount = await context.Messages
                .Where(m => m.Id == messageId && m.ChannelId == channelId)
                .ExecuteDeleteAsync();

            return deletedCount > 0;
        }

        public async Task<int> ClearChannelMessagesAsync(string channelId)
        {
            using var context = CreateContext();

            return await context.Messages
                .Where(m => m.ChannelId == channelId)
                .ExecuteDeleteAsync();
        }

        public async Task<int> GetMessageCountAsync(string? channelId = null)
        {
            using var context = CreateContext();

            if (!string.IsNullOrEmpty(channelId))
            {
                return await context.Messages.CountAsync(m => m.ChannelId == channelId);
            }

            return await context.Messages.CountAsync();
        }

        private Dictionary<string, object> MapEntityToDictionary(MessageEntity entity)
        {
            var dict = new Dictionary<string, object>
            {
                { "id", entity.Id },
                { "message_id", entity.Id },
                { "channel_id", entity.ChannelId },
                { "clan_id", entity.ClanId! },
                { "sender_id", entity.SenderId! },
                { "topic_id", entity.TopicId! },
                { "create_time_seconds", entity.CreateTimeSeconds! },
                
                { "content", DeserializeJson(entity.Content, new Dictionary<string, object>()) },
                { "mentions", DeserializeJson(entity.Mentions, new List<object>()) },
                { "attachments", DeserializeJson(entity.Attachments, new List<object>()) },
                { "reactions", DeserializeJson(entity.Reactions, new List<object>()) },
                { "references", DeserializeJson(entity.MsgReferences, new List<object>()) }
            };

            return dict;
        }

        private object DeserializeJson(string? jsonString, object defaultValue)
        {
            if (string.IsNullOrEmpty(jsonString))
                return defaultValue;

            try
            {
                return JsonSerializer.Deserialize<object>(jsonString) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private string SerializeJson(Dictionary<string, object> source, string key, object defaultValue)
        {
            var value = source.GetValueOrDefault(key) ?? defaultValue;
            return JsonSerializer.Serialize(value);
        }

        public async Task CloseAsync()
        {
            if (_masterConnection.State == System.Data.ConnectionState.Open)
            {
                await _masterConnection.CloseAsync();
                await _masterConnection.DisposeAsync();
                Logger.Debug("Master Database Connection closed.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
        }
    }
}
