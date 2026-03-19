namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class StartEndIndex
    {
        [JsonPropertyName("s")]
        public int? Start { get; set; }

        [JsonPropertyName("e")]
        public int? End { get; set; }

    }
}
