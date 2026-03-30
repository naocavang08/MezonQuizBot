using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mezon_sdk.Constants;

namespace Mezon_sdk.Utils
{
    public static class Helper
    {
        private static int _sequence = 0;
        private static long _lastTimestamp = 0;
        private static readonly object _lock = new object();

        public static int ConvertChannelTypeToChannelMode(int? channelType)
        {
            switch (channelType)
            {
                case (int)ChannelType.ChannelTypeDm:
                    return (int)ChannelStreamMode.StreamModeDm;
                case (int)ChannelType.ChannelTypeGroup:
                    return (int)ChannelStreamMode.StreamModeGroup;
                case (int)ChannelType.ChannelTypeChannel:
                    return (int)ChannelStreamMode.StreamModeChannel;
                case (int)ChannelType.ChannelTypeThread:
                    return (int)ChannelStreamMode.StreamModeThread;
                default:
                    return 0;
            }
        }

        public static bool IsValidUserId(object userId)
        {
            if (userId is string strId)
            {
                return Regex.IsMatch(strId, @"^\d+$");
            }
            if (userId is int || userId is long || userId is double)
            {
                return true;
            }
            return false;
        }

        public static async Task SleepAsync(int ms)
        {
            await Task.Delay(ms);
        }

        public static (string Host, string Port, bool UseSSL) ParseUrlToHostAndSsl(string urlStr)
        {
            var uri = new Uri(urlStr);
            var host = uri.Host;
            var isHttps = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            
            var port = uri.IsDefaultPort ? (isHttps ? "443" : "80") : uri.Port.ToString();

            return (host, port, isHttps);
        }

        public static long GenerateSnowflakeId()
        {
            lock (_lock)
            {
                long epoch = 1577836800000; // Custom epoch
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (timestamp == _lastTimestamp)
                {
                    _sequence++;
                }
                else
                {
                    _sequence = 0;
                    _lastTimestamp = timestamp;
                }

                long workerId = 1;
                long datacenterId = 1;

                long snowflakeId = ((timestamp - epoch) << 22)
                    | (((long)(uint)datacenterId) << 17)
                    | (((long)(uint)workerId) << 12)
                    | (uint)_sequence;

                return snowflakeId;
            }
        }

        public static int? ToInt(object? value)
        {
            if (value == null) return null;

            try
            {
                if (value is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Number)
                        return je.GetInt32();

                    if (je.ValueKind == JsonValueKind.String &&
                        int.TryParse(je.GetString(), out var i))
                        return i;
                }

                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }
    }

    public static class ProtoUtils
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static T? FromProtobuf<T>(IMessage message)
        {
            var json = JsonFormatter.Default.Format(message);
            var normalizedJson = NormalizeProtobufJson(json);

            return JsonSerializer.Deserialize<T>(normalizedJson, SerializerOptions);
        }

        private static string NormalizeProtobufJson(string json)
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            NormalizeNode(node);
            return node.ToJsonString();
        }

        private static void NormalizeNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var renamed = new List<(string OldName, string NewName, JsonNode? Value)>();
                foreach (var property in obj)
                {
                    var normalizedName = ToSnakeCase(property.Key);
                    if (property.Value is not null)
                    {
                        NormalizeNode(property.Value);
                    }

                    if (!string.Equals(property.Key, normalizedName, StringComparison.Ordinal))
                    {
                        renamed.Add((property.Key, normalizedName, property.Value));
                    }
                }

                foreach (var item in renamed)
                {
                    obj.Remove(item.OldName);
                    obj[item.NewName] = item.Value;
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        NormalizeNode(item);
                    }
                }
            }
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var builder = new System.Text.StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (char.IsUpper(current))
                {
                    if (i > 0)
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }
    }
}
