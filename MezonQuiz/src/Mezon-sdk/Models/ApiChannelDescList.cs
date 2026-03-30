namespace Mezon_sdk.Models
{
    using Mezon.Protobuf;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiChannelDescList : MezonBaseModel<ApiChannelDescList>
    {
        [JsonPropertyName("channeldesc")]
        public List<ApiChannelDescription>? Channeldesc { get; set; }

        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

        public static ApiChannelDescList FromProtobuf(ChannelDescList message)
        {
            var result = Mezon_sdk.Utils.ProtoUtils.FromProtobuf<ApiChannelDescList>(message)
                ?? new ApiChannelDescList();

            if (result.Channeldesc is { Count: > 0 } && string.IsNullOrWhiteSpace(result.Cursor))
            {
                result.Cursor = $"cursor-{result.Channeldesc.Count}";
            }

            return result;
        }
    }
}
