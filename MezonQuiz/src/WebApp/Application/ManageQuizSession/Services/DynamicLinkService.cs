using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QRCoder;
using WebApp.Application.ManageQuizSession.Dtos;

namespace WebApp.Application.ManageQuizSession.Services
{
    public class DynamicLinkService : IDynamicLinkService
    {
        private const int CodeLength = 6;
        private const string CodeChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly IConfiguration _configuration;
        public DynamicLinkService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SessionLinksDto BuildSessionLinks(Guid sessionId)
        {
            var input = new DataJsonDto
            {
                Id = _configuration["MezonBot:BotId"] ?? string.Empty,
                Name = _configuration["MezonBot:BotName"] ?? "MezonQuizBot-5559",
                Avatar = _configuration["MezonBot:BotAvatar"] ?? "https://cdn.mezon.vn/1967925734009737216/2032376880174206976.jpg"
            };

            string dataJson = JsonSerializer.Serialize(input);
            string urlEncoded = WebUtility.UrlEncode(dataJson);
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(urlEncoded));

            string baseUrl = _configuration["MezonBot:BaseLink"] ?? "https://mezon.ai/chat";
            string linkForQr = $"{baseUrl}/{input.Name}?data={base64Encoded}";

            string qrCodeDataUri = GenerateQrCodeDataUri(linkForQr);

            return new SessionLinksDto
            {
                DeepLink = linkForQr,
                QrCodeUrl = qrCodeDataUri,
                Code = EncodeSessionCode(sessionId)
            };
        }

        /// <summary>
        /// Mã hóa sessionId thành mã ngắn 6 ký tự (0-9, A-Z) bằng SHA256 + base36.
        /// Deterministic: cùng sessionId luôn cho ra cùng Code.
        /// </summary>
        private static string EncodeSessionCode(Guid sessionId)
        {
            byte[] hash = SHA256.HashData(sessionId.ToByteArray());

            // Lấy 8 byte đầu của hash → chuyển thành số dương
            var number = new BigInteger(hash.AsSpan(0, 8), isUnsigned: true);

            var result = new char[CodeLength];
            int baseN = CodeChars.Length; // 36

            for (int i = CodeLength - 1; i >= 0; i--)
            {
                number = BigInteger.DivRem(number, baseN, out var remainder);
                result[i] = CodeChars[(int)remainder];
            }

            return new string(result);
        }

        private static string GenerateQrCodeDataUri(string url)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);

            byte[] qrCodeBytes = qrCode.GetGraphic(10);
            string base64 = Convert.ToBase64String(qrCodeBytes);

            return $"data:image/png;base64,{base64}";
        }
    }
}
