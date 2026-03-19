namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiMessageAttachment : MezonBaseModel<ApiMessageAttachment>
    {
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("filetype")]
        public string? Filetype { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("channel_label")]
        public string? ChannelLabel { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

        [JsonPropertyName("sender_id")]
        public int? SenderId { get; set; }

    }
}
