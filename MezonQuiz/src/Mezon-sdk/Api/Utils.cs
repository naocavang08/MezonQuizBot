namespace Mezon_sdk.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public static class Utils
    {
        private static readonly string[] BinaryContentTypes = new[]
        {
            "application/proto",
            "application/x-protobuf",
            "application/protobuf",
            "application/octet-stream",
            "text/plain; charset=utf-8"
        };

        public static string BuildUrl(
            string scheme,
            string host,
            string port = "",
            string path = "",
            string paramsStr = "",
            IDictionary<string, object?>? query = null,
            string fragment = "")
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = host
            };

            if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portInt))
            {
                uriBuilder.Port = portInt;
            }

            if (!string.IsNullOrEmpty(path))
            {
                uriBuilder.Path = path;
            }

            if (!string.IsNullOrEmpty(fragment))
            {
                uriBuilder.Fragment = fragment;
            }

            if (query != null && query.Count > 0)
            {
                var queryParams = query
                    .Where(kv => kv.Value != null)
                    .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!.ToString()!)}");
                uriBuilder.Query = string.Join("&", queryParams);
            }

            return uriBuilder.ToString();
        }

        public static string BuildBody(object body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(body, options);
        }

        public static (string Scheme, string Hostname, bool UseSsl, string Port) ParseUrlComponents(string url, bool useSsl = false)
        {
            var uri = new Uri(url);
            var scheme = uri.Scheme;
            useSsl = useSsl || IsSchemaSecure(scheme);
            
            var port = uri.IsDefaultPort ? (useSsl ? "443" : "80") : uri.Port.ToString();

            return (scheme, uri.Host, useSsl, port);
        }

        public static bool IsSchemaSecure(string schema)
        {
            return schema.Equals("https", StringComparison.OrdinalIgnoreCase) || 
                   schema.Equals("wss", StringComparison.OrdinalIgnoreCase);
        }

        public static IDictionary<string, object> BuildParams(IDictionary<string, object?>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return new Dictionary<string, object>();
            }

            return parameters
                .Where(kv => kv.Value != null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!);
        }

        public static bool IsBinaryResponse(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            return BinaryContentTypes.Any(t => contentType.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
    }
}