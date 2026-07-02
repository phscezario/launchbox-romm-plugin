using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RommPlugin.Core.Services
{
    public class ConnectionTestResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }
    }

    public static class RommConnectionTester
    {
        public static async Task<ConnectionTestResult> TestAsync(
            string baseUrl,
            string clientApiToken,
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Base URL is required."
                };
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Base URL is not a valid absolute URL."
                };
            }

            using (var http = new HttpClient
            {
                BaseAddress = baseUri,
                Timeout = TimeSpan.FromSeconds(15)
            })
            {
                if (!string.IsNullOrWhiteSpace(clientApiToken))
                {
                    http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", clientApiToken.Trim());
                }
                else
                {
                    var credentials = $"{username}:{password}";
                    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                    http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", base64);
                }

                try
                {
                    var response = await http.GetAsync("/api/platforms");

                    if (response.IsSuccessStatusCode)
                    {
                        return new ConnectionTestResult
                        {
                            Success = true,
                            Message = "Connection successful."
                        };
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        return new ConnectionTestResult
                        {
                            Success = false,
                            Message = "Authentication failed - check your token or username/password."
                        };
                    }

                    return new ConnectionTestResult
                    {
                        Success = false,
                        Message = $"Server returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                    };
                }
                catch (Exception ex)
                {
                    return new ConnectionTestResult
                    {
                        Success = false,
                        Message = "Could not reach the server: " + ex.Message
                    };
                }
            }
        }
    }
}
