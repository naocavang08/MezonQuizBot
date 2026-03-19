namespace Mezon_sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ApiClanDesc : MezonBaseModel<ApiClanDesc>
    {
        [JsonPropertyName("banner")]
        public string? Banner { get; set; }

        [JsonPropertyName("clan_id")]
        public int? ClanId { get; set; }

        [JsonPropertyName("clan_name")]
        public string? ClanName { get; set; }

        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        [JsonPropertyName("logo")]
        public string? Logo { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("badge_count")]
        public int? BadgeCount { get; set; }

        [JsonPropertyName("is_onboarding")]
        public bool? IsOnboarding { get; set; }

        [JsonPropertyName("welcome_channel_id")]
        public int? WelcomeChannelId { get; set; }

        [JsonPropertyName("onboarding_banner")]
        public string? OnboardingBanner { get; set; }

    }

    public class ApiClanDescList : MezonBaseModel<ApiClanDescList>
    {
        [JsonPropertyName("clandesc")]
        public List<ApiClanDesc>? Clandesc { get; set; }

    }
}
