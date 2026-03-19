namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiAuthenticateRequest : MezonBaseModel<ApiAuthenticateRequest>
    {
        [JsonPropertyName("account")]
        public ApiAccountApp? Account { get; set; }

    }
}
