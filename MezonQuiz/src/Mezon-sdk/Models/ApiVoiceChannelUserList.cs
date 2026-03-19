namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiVoiceChannelUserList : MezonBaseModel<ApiVoiceChannelUserList>
    {
        [JsonPropertyName("voice_channel_users")]
        public List<ApiVoiceChannelUser>? VoiceChannelUsers { get; set; }

    }
}
