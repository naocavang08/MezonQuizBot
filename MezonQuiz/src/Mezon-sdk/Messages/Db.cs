using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using Mezon_sdk.Models;
using Mezon_sdk.Utils;

namespace Mezon_sdk.Messages
{
    /// <summary>
    /// Async SQL Server-based message database for caching Mezon messages.
    /// </summary>
    public class MessageDb : IAsyncDisposable
    {
        private readonly string _connectionString;
        private SqlConnection? _db;
        private bool _initialized;
        private static readonly Logger Logger = new Logger("MessageDB");

        public MessageDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task EnsureConnectionAsync()
        {
            if (_db == null || !_initialized)
            {
                _db = new SqlConnection(_connectionString);
                await _db.OpenAsync();
                await InitTablesAsync();
                _initialized = true;
            }
            else if (_db.State != System.Data.ConnectionState.Open)
            {
                await _db.OpenAsync();
            }
        }

        private async Task InitTablesAsync()
        {
            if (_db == null) return;

            var createTableCmd = _db.CreateCommand();
            createTableCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='messages' AND xtype='U')
                BEGIN
                    CREATE TABLE messages (
                        id NVARCHAR(255) NOT NULL,
                        channel_id NVARCHAR(255) NOT NULL,
                        clan_id NVARCHAR(255),
                        sender_id NVARCHAR(255),
                        content NVARCHAR(MAX),
                        mentions NVARCHAR(MAX),
                        attachments NVARCHAR(MAX),
                        reactions NVARCHAR(MAX),
                        msg_references NVARCHAR(MAX),
                        topic_id NVARCHAR(255),
                        create_time_seconds BIGINT,
                        CONSTRAINT PK_messages PRIMARY KEY (id, channel_id)
                    )
                END";
            await createTableCmd.ExecuteNonQueryAsync();

            var createIndexCmd = _db.CreateCommand();
            createIndexCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_messages_channel_id' AND object_id = OBJECT_ID('messages'))
                BEGIN
                    CREATE INDEX idx_messages_channel_id ON messages(channel_id)
                END";
            await createIndexCmd.ExecuteNonQueryAsync();

            Logger.Debug("Database tables initialized");
        }

        public async Task SaveMessageAsync(Dictionary<string, object> message)
        {
            await EnsureConnectionAsync();
            if (_db == null) return;

            var cmd = _db.CreateCommand();
            // SQL Server UPSERT (MERGE)
            cmd.CommandText = @"
                MERGE INTO messages WITH (HOLDLOCK) AS target
                USING (SELECT @id AS id, @channel_id AS channel_id) AS source
                ON target.id = source.id AND target.channel_id = source.channel_id
                WHEN MATCHED THEN 
                    UPDATE SET 
                        clan_id = @clan_id, 
                        sender_id = @sender_id,
                        content = @content, 
                        mentions = @mentions, 
                        attachments = @attachments, 
                        reactions = @reactions,
                        msg_references = @msg_references, 
                        topic_id = @topic_id, 
                        create_time_seconds = @create_time_seconds
                WHEN NOT MATCHED THEN
                    INSERT (id, clan_id, channel_id, sender_id, content, mentions, attachments, reactions, msg_references, topic_id, create_time_seconds)
                    VALUES (@id, @clan_id, @channel_id, @sender_id, @content, @mentions, @attachments, @reactions, @msg_references, @topic_id, @create_time_seconds);";

            cmd.Parameters.AddWithValue("@id", message.GetValueOrDefault("message_id")?.ToString() ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@clan_id", message.GetValueOrDefault("clan_id")?.ToString() ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@channel_id", message.GetValueOrDefault("channel_id")?.ToString() ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sender_id", message.GetValueOrDefault("sender_id")?.ToString() ?? (object)DBNull.Value);
            
            cmd.Parameters.AddWithValue("@content", JsonSerializer.Serialize(message.GetValueOrDefault("content") ?? new Dictionary<string, object>()));
            cmd.Parameters.AddWithValue("@mentions", JsonSerializer.Serialize(message.GetValueOrDefault("mentions") ?? new List<object>()));
            cmd.Parameters.AddWithValue("@attachments", JsonSerializer.Serialize(message.GetValueOrDefault("attachments") ?? new List<object>()));
            cmd.Parameters.AddWithValue("@reactions", JsonSerializer.Serialize(message.GetValueOrDefault("reactions") ?? new List<object>()));
            cmd.Parameters.AddWithValue("@msg_references", JsonSerializer.Serialize(message.GetValueOrDefault("references") ?? new List<object>()));
            
            cmd.Parameters.AddWithValue("@topic_id", message.GetValueOrDefault("topic_id")?.ToString() ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@create_time_seconds", message.GetValueOrDefault("create_time_seconds") ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            Logger.Debug($"Saved message {message.GetValueOrDefault("message_id")} in channel {message.GetValueOrDefault("channel_id")}");
        }

        public async Task<ChannelMessage?> GetMessageByIdAsync(string messageId, string channelId)
        {
            await EnsureConnectionAsync();
            if (_db == null) return null;

            var cmd = _db.CreateCommand();
            cmd.CommandText = @"
                SELECT TOP 1 * FROM messages
                WHERE channel_id = @channel_id AND id = @id";
            cmd.Parameters.AddWithValue("@channel_id", channelId);
            cmd.Parameters.AddWithValue("@id", messageId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.GetValue(i);
                }
                
                var json = JsonSerializer.Serialize(dict);
                return JsonSerializer.Deserialize<ChannelMessage>(json);
            }

            return null;
        }

        public async Task<List<Dictionary<string, object>>> GetMessagesByChannelAsync(string channelId, int limit = 50, int offset = 0)
        {
            await EnsureConnectionAsync();
            if (_db == null) return new List<Dictionary<string, object>>();

            var cmd = _db.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM messages
                WHERE channel_id = @channel_id
                ORDER BY create_time_seconds DESC
                OFFSET @offset ROWS
                FETCH NEXT @limit ROWS ONLY";
            cmd.Parameters.AddWithValue("@channel_id", channelId);
            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            var messages = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var message = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = reader.IsDBNull(i) ? null : reader.GetString(i);
                    
                    if (val != null && (name == "content" || name == "mentions" || name == "attachments" || name == "reactions" || name == "msg_references"))
                    {
                        message[name] = JsonSerializer.Deserialize<object>(val) ?? new object();
                    }
                    else
                    {
                        message[name] = reader.GetValue(i);
                    }
                }
                
                if (message.ContainsKey("msg_references"))
                {
                    message["references"] = message["msg_references"];
                    message.Remove("msg_references");
                }
                
                messages.Add(message);
            }

            return messages;
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string channelId)
        {
            await EnsureConnectionAsync();
            if (_db == null) return false;

            var cmd = _db.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM messages
                WHERE id = @id AND channel_id = @channel_id";
            cmd.Parameters.AddWithValue("@id", messageId);
            cmd.Parameters.AddWithValue("@channel_id", channelId);

            var deletedCount = await cmd.ExecuteNonQueryAsync();
            var deleted = deletedCount > 0;

            if (deleted)
            {
                Logger.Debug($"Deleted message {messageId} from channel {channelId}");
            }

            return deleted;
        }

        public async Task<int> ClearChannelMessagesAsync(string channelId)
        {
            await EnsureConnectionAsync();
            if (_db == null) return 0;

            var cmd = _db.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM messages
                WHERE channel_id = @channel_id";
            cmd.Parameters.AddWithValue("@channel_id", channelId);

            var deletedCount = await cmd.ExecuteNonQueryAsync();
            Logger.Info($"Cleared {deletedCount} messages from channel {channelId}");
            return deletedCount;
        }

        public async Task<int> GetMessageCountAsync(string? channelId = null)
        {
            await EnsureConnectionAsync();
            if (_db == null) return 0;

            var cmd = _db.CreateCommand();
            if (channelId != null)
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM messages
                    WHERE channel_id = @channel_id";
                cmd.Parameters.AddWithValue("@channel_id", channelId);
            }
            else
            {
                cmd.CommandText = "SELECT COUNT(*) FROM messages";
            }

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task CloseAsync()
        {
            if (_db != null)
            {
                await _db.CloseAsync();
                await _db.DisposeAsync();
                _db = null;
                _initialized = false;
                Logger.Debug("Database connection closed");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
        }
    }
}