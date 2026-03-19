namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public enum MessageSelectType
    {
        Text = 1,
        User = 2,
        Role = 3,
        Channel = 4,
    }
}
