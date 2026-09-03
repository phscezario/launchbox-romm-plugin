using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Provides helper methods for applying HTTP authentication headers to <see cref="HttpClient"/> instances.
    /// Supports both Bearer token and Basic authentication schemes.
    /// </summary>
    public static class AuthHeaderHelper
    {
        /// <summary>
        /// Applies the appropriate authentication header to the HTTP client.
        /// If an API token is provided, uses Bearer authentication.
        /// Otherwise, if username and password are provided, uses Basic authentication.
        /// </summary>
        /// <param name="http">The HTTP client to configure.</param>
        /// <param name="apiToken">The RomM API token (rmm_...). Takes priority over username/password.</param>
        /// <param name="username">The RomM username for basic authentication.</param>
        /// <param name="password">The RomM password for basic authentication.</param>
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
