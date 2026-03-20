using WebApp.Application.Interface;

namespace WebApp.Application.Services
{
    public class DynamicLinkService : IDynamicLinkService
    {
        private readonly IConfiguration _configuration;

        public DynamicLinkService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string DeepLink, string QrCodeUrl) BuildSessionLinks(Guid sessionId, Guid quizId, Guid hostId)
        {
            var targetBaseUrl = (_configuration["DynamicLink:TargetBaseUrl"] ?? "https://mezonquiz.app/session").TrimEnd('/');
            var dynamicLinkDomain = (_configuration["DynamicLink:Domain"] ?? "https://mezonquiz.page.link").TrimEnd('/');
            var useTargetAsDeepLink = bool.TryParse(_configuration["DynamicLink:UseTargetAsDeepLink"], out var parsedUseTargetAsDeepLink)
                && parsedUseTargetAsDeepLink;
            var androidPackage = _configuration["DynamicLink:AndroidPackage"] ?? string.Empty;
            var iosBundle = _configuration["DynamicLink:IOSBundle"] ?? string.Empty;
            var iosAppStoreId = _configuration["DynamicLink:IOSAppStoreId"] ?? string.Empty;
            var qrTemplate = _configuration["DynamicLink:QrCodeApiTemplate"]
                             ?? "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={0}";

            var targetLink = $"{targetBaseUrl}/{sessionId}?quizId={quizId}&hostId={hostId}";

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
    }
}
