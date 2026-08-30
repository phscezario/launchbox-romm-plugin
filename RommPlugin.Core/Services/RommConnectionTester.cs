using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RommPlugin.Core.Locale;

namespace RommPlugin.Core.Services
{
    public class RommConnectionTester
    {
        private readonly HttpClient _http;

        public RommConnectionTester() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
        {
        }

        public RommConnectionTester(HttpClient httpClient)
        {
            _http = httpClient;
        }

        public async Task<ConnectionTestResult> TestAsync(
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
                    Message = LocaleManager.Get("connection.url_required")
                };
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = LocaleManager.Get("connection.url_invalid")
                };
            }

            try
            {
                var requestUri = new Uri(baseUri, "/api/platforms");
                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    if (!string.IsNullOrWhiteSpace(clientApiToken))
                    {
                        request.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clientApiToken.Trim());
                    }
                    else if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        var credentials = $"{username}:{password}";
                        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
                        request.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
                    }

                    using (var response = await _http.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return new ConnectionTestResult
                            {
                                Success = true,
                                Message = LocaleManager.Get("connection.success")
                            };
                        }

                        if (response.StatusCode == HttpStatusCode.Unauthorized ||
                            response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            return new ConnectionTestResult
                            {
                                Success = false,
                                Message = LocaleManager.Get("connection.auth_failed")
                            };
                        }

                        return new ConnectionTestResult
                        {
                            Success = false,
                            Message = LocaleManager.Get("connection.server_error", ((int)response.StatusCode).ToString(), response.ReasonPhrase ?? "")
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = LocaleManager.Get("connection.unreachable") + " " + ex.Message
                };
            }
        }
    }
}
