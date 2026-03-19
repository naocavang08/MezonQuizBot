namespace Mezon_sdk.Models
{
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
}
