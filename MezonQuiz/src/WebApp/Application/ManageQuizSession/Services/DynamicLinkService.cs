namespace WebApp.Application.ManageQuizSession.Services
{
    public class DynamicLinkService : IDynamicLinkService
    {
        private const string DefaultPublicTargetBaseUrl = "http://10.10.31.109:5173/app/sessions";
        private readonly IConfiguration _configuration;

        public DynamicLinkService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string DeepLink, string QrCodeUrl) BuildSessionLinks(Guid sessionId, Guid quizId, Guid hostId)
        {
            var configuredTargetBaseUrl = (_configuration["DynamicLink:TargetBaseUrl"] ?? DefaultPublicTargetBaseUrl).TrimEnd('/');
            var publicTargetBaseUrl = (_configuration["DynamicLink:PublicTargetBaseUrl"] ?? DefaultPublicTargetBaseUrl).TrimEnd('/');
            var targetBaseUrl = ResolveTargetBaseUrl(configuredTargetBaseUrl, publicTargetBaseUrl);
            var dynamicLinkDomain = (_configuration["DynamicLink:Domain"] ?? "https://mezonquiz.page.link").TrimEnd('/');
            var useTargetAsDeepLink = bool.TryParse(_configuration["DynamicLink:UseTargetAsDeepLink"], out var parsedUseTargetAsDeepLink)
                && parsedUseTargetAsDeepLink;
            var androidPackage = _configuration["DynamicLink:AndroidPackage"] ?? string.Empty;
            var iosBundle = _configuration["DynamicLink:IOSBundle"] ?? string.Empty;
            var iosAppStoreId = _configuration["DynamicLink:IOSAppStoreId"] ?? string.Empty;
            var qrTemplate = _configuration["DynamicLink:QrCodeApiTemplate"]
                             ?? "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={0}";

            var targetLink = $"{targetBaseUrl}/{sessionId}/play?quizId={quizId}&hostId={hostId}";

            if (useTargetAsDeepLink)
            {
                var directQrCodeUrl = string.Format(qrTemplate, Uri.EscapeDataString(targetLink));
                return (targetLink, directQrCodeUrl);
            }

            var query = new List<string>
            {
                $"link={Uri.EscapeDataString(targetLink)}"
            };

            if (!string.IsNullOrWhiteSpace(androidPackage))
            {
                query.Add($"apn={Uri.EscapeDataString(androidPackage)}");
            }

            if (!string.IsNullOrWhiteSpace(iosBundle))
            {
                query.Add($"ibi={Uri.EscapeDataString(iosBundle)}");
            }

            if (!string.IsNullOrWhiteSpace(iosAppStoreId))
            {
                query.Add($"isi={Uri.EscapeDataString(iosAppStoreId)}");
            }

            var deepLink = $"{dynamicLinkDomain}/?{string.Join("&", query)}";
            var qrCodeUrl = string.Format(qrTemplate, Uri.EscapeDataString(deepLink));

            return (deepLink, qrCodeUrl);
        }

        private static string ResolveTargetBaseUrl(string configuredTargetBaseUrl, string publicTargetBaseUrl)
        {
            if (Uri.TryCreate(configuredTargetBaseUrl, UriKind.Absolute, out var configuredUri))
            {
                var isLocalHost = configuredUri.IsLoopback || string.Equals(configuredUri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
                if (!isLocalHost)
                {
                    return configuredTargetBaseUrl;
                }
            }

            return publicTargetBaseUrl;
        }
    }
}
