namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ChannelMessageContent
    {
        [JsonPropertyName("t")]
        public string? Text { get; set; }

        [JsonPropertyName("contentThread")]
        public string? ContentThread { get; set; }

        [JsonPropertyName("hg")]
        public List<HashtagOnMessage>? Hashtags { get; set; }

        [JsonPropertyName("ej")]
        public List<EmojiOnMessage>? Emojis { get; set; }

        [JsonPropertyName("lk")]
        public List<LinkOnMessage>? Links { get; set; }

        [JsonPropertyName("mk")]
        public List<MarkdownOnMessage>? Markdown { get; set; }

        [JsonPropertyName("vk")]
        public List<LinkVoiceRoomOnMessage>? VoiceLinks { get; set; }

        [JsonPropertyName("embed")]
        public List<InteractiveMessageProps>? Embed { get; set; }

        [JsonPropertyName("components")]
        public List<MessageActionRow>? Components { get; set; }

    }
}
