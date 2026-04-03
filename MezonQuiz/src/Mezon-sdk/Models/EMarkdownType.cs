namespace Mezon_sdk.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public enum EMarkdownType
    {
        Triple,
        Single,
        Pre,
        Code,
        Bold,
        Link,
        VoiceLink,
        LinkYoutube,
    }

    public static class EMarkdownTypeExtensions
    {
        public static string ToWireValue(this EMarkdownType t) => t switch
        {
            EMarkdownType.Triple => "t",
            EMarkdownType.Single => "s",
            EMarkdownType.Pre => "pre",
            EMarkdownType.Code => "c",
            EMarkdownType.Bold => "b",
            EMarkdownType.Link => "lk",
            EMarkdownType.VoiceLink => "vk",
            EMarkdownType.LinkYoutube => "lk_yt",
            _ => ""
        };
    }

    public sealed class EMarkdownTypeWireConverter : JsonConverter<EMarkdownType?>
    {
        public override EMarkdownType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return value switch
                {
                    "t" => EMarkdownType.Triple,
                    "s" => EMarkdownType.Single,
                    "pre" => EMarkdownType.Pre,
                    "c" => EMarkdownType.Code,
                    "b" => EMarkdownType.Bold,
                    "lk" => EMarkdownType.Link,
                    "vk" => EMarkdownType.VoiceLink,
                    "lk_yt" => EMarkdownType.LinkYoutube,
                    _ => null
                };
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var enumValue))
            {
                if (Enum.IsDefined(typeof(EMarkdownType), enumValue))
                {
                    return (EMarkdownType)enumValue;
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, EMarkdownType? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToWireValue());
        }
    }
}
