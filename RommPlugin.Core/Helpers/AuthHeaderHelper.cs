using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace RommPlugin.Core.Helpers
{
    public static class AuthHeaderHelper
    {
        public static void ApplyAuthentication(HttpClient http, string apiToken, string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(apiToken))
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiToken.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var credentials = $"{username}:{password}";
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", base64);
            }
        }
    }
}
