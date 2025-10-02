using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace Common.Helper
{
    public static class LogCurlApiHelper
    {
        public static void LogCurl(HttpRequestMessage request, string? body)
        {
            var curl = BuildCurl(request, body);
            Debug.WriteLine(curl);
            Console.WriteLine(curl);
        }

        private static string BuildCurl(HttpRequestMessage request, string? body)
        {
            var sb = new StringBuilder();
            sb.Append("curl --location ");
            sb.Append('\'').Append(request.RequestUri!.ToString()).Append('\'');
            sb.Append(" \\\n--request ").Append(request.Method.Method);

            // Headers
            foreach (var header in request.Headers)
            {
                foreach (var val in header.Value)
                {
                    sb.Append(" \\\n--header '")
                      .Append(header.Key).Append(": ").Append(val).Append('\'');
                }
            }
            if (request.Content?.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    foreach (var val in header.Value)
                    {
                        sb.Append(" \\\n--header '")
                          .Append(header.Key).Append(": ").Append(val).Append('\'');
                    }
                }
            }

            // Body (mask secrets nếu là JSON)
            if (!string.IsNullOrWhiteSpace(body) && IsJson(body))
            {
                var masked = MaskSecrets(body);
                sb.Append(" \\\n--data-raw '").Append(masked.Replace("'", "\\'")).Append('\'');
            }
            else if (!string.IsNullOrWhiteSpace(body))
            {
                sb.Append(" \\\n--data-raw '").Append(body.Replace("'", "\\'")).Append('\'');
            }

            return sb.ToString();
        }

        private static bool IsJson(string s)
        {
            s = s.Trim();
            return (s.StartsWith("{") && s.EndsWith("}")) || (s.StartsWith("[") && s.EndsWith("]"));
        }

        private static string MaskSecrets(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json);
                if (obj == null) return json;

                void Mask(string key)
                {
                    if (obj.ContainsKey(key) && obj[key] is string str && !string.IsNullOrEmpty(str))
                    {
                        obj[key] = new string('*', Math.Min(8, str.Length));
                    }
                }

                Mask("password");
                Mask("client_secret");

                return JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return json;
            }
        }
    }
}
