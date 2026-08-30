using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.ApiClient
{
    public class ClientErrorException : Exception
    {
        public int StatusCode { get; }

        public ClientErrorException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class RommApiClient : IRommApiClient
    {
        private readonly HttpClient _http;

        public RommApiClient(string baseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/')),
                Timeout = TimeSpan.FromSeconds(RommConstants.HttpTimeoutSeconds)
            };
        }

        internal RommApiClient(HttpClient httpClient)
        {
            _http = httpClient;
        }

        public void Dispose()
        {
            _http?.Dispose();
        }

        public void SetBasicAuthentication(string username, string password)
        {
            var credentials = $"{username}:{password}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", base64);
        }

        public void SetBearerAuthentication(string token)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public void ApplyAuthentication(RommPluginSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.ClientApiToken))
            {
                SetBearerAuthentication(settings.ClientApiToken.Trim());
            }
            else
            {
                SetBasicAuthentication(settings.Username, settings.Password);
            }
        }

        public async Task<List<RommPlatform>> GetPlatformsAsync()
        {
            using (var response = await _http.GetAsync("/api/platforms"))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<RommPlatform>>(json);
            }
        }

        public async Task<List<RommGame>> GetAllGamesByPlatformAsync(int platformId)
        {
            var allGames = new List<RommGame>();
            int limit = RommConstants.ApiPageSize;
            int offset = 0;
            bool hasMore = true;

            RommLogger.Log($"Fetching games for platform {platformId}...");

            while (hasMore)
            {
                var url = $"/api/roms?" +
                          $"platform_ids={platformId}&" +
                          $"order_by=name&order_dir=asc&" +
                          $"limit={limit}&offset={offset}&" +
                          $"with_rom_id_index=false&" +
                          $"with_char_index=false&" +
                          $"with_filter_values=false";

                using (var response = await _http.GetAsync(url))
                {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var rommResponse = JsonConvert.DeserializeObject<RommGameResponse>(json);

                if (rommResponse?.Items != null && rommResponse.Items.Count > 0)
                {
                    allGames.AddRange(rommResponse.Items);
                    offset += rommResponse.Items.Count;
                    RommLogger.Log($"Fetched {allGames.Count}/{rommResponse.Total} games (page offset={offset})");

                    if (offset >= rommResponse.Total)
                    {
                        hasMore = false;
                    }
                }
                else
                {
                    hasMore = false;
                }
                }
            }

            RommLogger.Log($"Total games fetched for platform {platformId}: {allGames.Count}");
            return allGames;
        }

        public async Task<RommGame> GetGameByIdAsync(int gameId)
        {
            using (var response = await _http.GetAsync($"/api/roms/{gameId}"))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RommGame>(json);
            }
        }

        public async Task UpdateGameById(int gameId, RommUpdateGameRequest request)
        {
            const int maxAttempts = RommConstants.MaxRetryAttempts;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (var content = new MultipartFormDataContent())
                    {
                    content.Add(new StringContent(request.Name ?? ""), "name");
                    content.Add(new StringContent(request.Summary ?? ""), "summary");
                    content.Add(new StringContent(request.LaunchboxId?.ToString() ?? ""), "launchbox_id");

                    if (request.RawLaunchboxMetadata != null)
                    {
                        ReplaceNullStrings(request.RawLaunchboxMetadata);
                    }

                    var launchboxJson = JsonConvert.SerializeObject(
                        request.RawLaunchboxMetadata,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }
                    );

                    content.Add(new StringContent(launchboxJson, Encoding.UTF8, "application/json"), "raw_launchbox_metadata");

                    if (!string.IsNullOrEmpty(request.ArtworkPath))
                    {
                        using (var fileStream = File.OpenRead(request.ArtworkPath))
                        {
                            var fileContent = new StreamContent(fileStream);

                            fileContent.Headers.ContentType =
                                new MediaTypeHeaderValue(GetMimeType(request.ArtworkPath));

                            content.Add(
                                fileContent,
                                "artwork",
                                SanitizeFileName(Path.GetFileName(request.ArtworkPath))
                            );

                            using (var response = await _http.PutAsync(
                                $"api/roms/{gameId}?remove_cover=false&unmatch_metadata=false",
                                content
                            ))
                            {
                            if (!response.IsSuccessStatusCode)
                            {
                                var statusCode = (int)response.StatusCode;
                                var isRetryable = statusCode >= 500 || statusCode == 499;

                                if (isRetryable)
                                {
                                    if (attempt == maxAttempts)
                                    {
                                        var body = await response.Content.ReadAsStringAsync();
                                        var detail = string.IsNullOrWhiteSpace(body) ? "" : $" - {body}";
                                        throw new HttpRequestException($"Server error {statusCode}{detail}");
                                    }

                                    await Task.Delay(500 * attempt);
                                    continue;
                                }

                                await ThrowIfNotSuccessAsync(response);
                            }

                            return;
                            }
                        }
                    }
                    else
                    {
                        using (var response = await _http.PutAsync(
                            $"api/roms/{gameId}?remove_cover=false&unmatch_metadata=false",
                            content
                        ))
                        {
                        if (!response.IsSuccessStatusCode)
                        {
                            var statusCode = (int)response.StatusCode;
                            var isRetryable = statusCode >= 500 || statusCode == 499;

                            if (isRetryable)
                            {
                                if (attempt == maxAttempts)
                                {
                                    var body = await response.Content.ReadAsStringAsync();
                                    var detail = string.IsNullOrWhiteSpace(body) ? "" : $" - {body}";
                                    throw new HttpRequestException($"Server error {statusCode}{detail}");
                                }

                                await Task.Delay(500 * attempt);
                                continue;
                            }

                            await ThrowIfNotSuccessAsync(response);
                        }

                        return;
                        }
                    }
                    }
                }
                catch (Exception ex) when (attempt < maxAttempts && IsTransientError(ex))
                {
                    RommLogger.LogError($"Connection error updating game {gameId} (attempt {attempt}/{maxAttempts}): {ex.Message}");
                    await Task.Delay(RommConstants.RetryBaseDelayMs * attempt);
                }
                catch (Exception ex) when (attempt == maxAttempts && IsTransientError(ex))
                {
                    RommLogger.LogError($"Connection error updating game {gameId} (all {maxAttempts} attempts failed): {ex.Message}");
                    throw;
                }
            }
        }

        public async Task RemoveGameMetadataById(int gameId)
        {
            const int maxAttempts = RommConstants.MaxRetryAttempts;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using (var response = await _http.PutAsync($"api/roms/{gameId}?remove_cover=false&unmatch_metadata=true", null))
                {
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var statusCode = (int)response.StatusCode;
                var isRetryable = statusCode >= 500 || statusCode == 499;

                if (isRetryable)
                {
                    if (attempt == maxAttempts)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        var detail = string.IsNullOrWhiteSpace(body) ? "" : $" - {body}";
                        throw new HttpRequestException($"Server error {statusCode}{detail}");
                    }

                    await Task.Delay(500 * attempt);
                    continue;
                }

                await ThrowIfNotSuccessAsync(response);
                }
            }
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
            url = url.Replace(" ", "%20");

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                using (var response = await _http.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }

            using (var relativeResponse = await _http.GetAsync(url))
            {
                relativeResponse.EnsureSuccessStatusCode();
                return await relativeResponse.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<int> UploadScreenshotAsync(int gameId, string filePath)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var fileName = SanitizeFileName(Path.GetFileName(filePath));
                var mimeType = GetMimeType(filePath);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RommConstants.UploadTimeoutSeconds)))
                using (var fileStream = File.OpenRead(filePath))
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                    content.Add(fileContent, "screenshotFile", fileName);

                    using (var response = await _http.PostAsync(
                        $"api/screenshots?rom_id={gameId}", content, cts.Token))
                    {
                        await ThrowIfNotSuccessAsync(response);

                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ScreenshotUploadResult>(json);
                        return result?.Id ?? 0;
                    }
                }
            });
        }

        public async Task SetScreenshotPublicAsync(int screenshotId)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RommConstants.HttpTimeoutSeconds)))
                {
                    var json = JsonConvert.SerializeObject(new { is_public = true });
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    using (var response = await _http.PutAsync(
                        $"api/screenshots/{screenshotId}", content, cts.Token))
                    {
                        await ThrowIfNotSuccessAsync(response);
                    }
                    return true;
                }
            });
        }

        public async Task<List<PlaySessionSchema>> GetPlaySessionsAsync(int romId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using (var response = await _http.GetAsync(
                    $"/api/play-sessions?rom_id={romId}&limit={RommConstants.ApiPageSize}"))
                {
                await ThrowIfNotSuccessAsync(response);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<PlaySessionSchema>>(json);
                }
            });
        }

        public async Task<PlaySessionIngestResponse> IngestPlaySessionsAsync(PlaySessionIngestPayload payload)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var json = JsonConvert.SerializeObject(payload);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await _http.PostAsync("/api/play-sessions", content))
                {
                    await ThrowIfNotSuccessAsync(response);
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PlaySessionIngestResponse>(responseJson);
                }
            });
        }

        public async Task UpdateGameLastPlayedAsync(int gameId)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                using (var response = await _http.PutAsync(
                    $"api/roms/{gameId}/props?update_last_played=true", null))
                {
                await ThrowIfNotSuccessAsync(response);
                return true;
                }
            });
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (ClientErrorException)
                {
                    throw;
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await Task.Delay(500 * (int)Math.Pow(2, attempt - 1));
                }
            }

            return default;
        }

        private static async Task ThrowIfNotSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var statusCode = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync();
            var detail = string.IsNullOrWhiteSpace(body) ? "" : $" - {body}";

            if (statusCode >= 400 && statusCode < 500)
            {
                throw new ClientErrorException(statusCode,
                    $"HTTP {statusCode}{detail}");
            }

            throw new HttpRequestException(
                $"Server error {statusCode}{detail}");
        }

        private static bool IsTransientError(Exception ex)
        {
            return ex is SocketException
                || ex is IOException
                || ex is TaskCanceledException
                || (ex is HttpRequestException hre
                    && (hre.InnerException is SocketException
                        || hre.InnerException is IOException));
        }

        private string SanitizeFileName(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);

            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (c <= 0x7F)
                {
                    sb.Append(c);
                }
            }

            var sanitized = sb.ToString().Trim('.', ' ');
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "file";
            }

            return sanitized + ext;
        }

        private string GetMimeType(string path)
        {
            var ext = Path.GetExtension(path).ToLower();

            if (ext == ".png")
            {
                return "image/png";
            }

            if (ext == ".jpg" || ext == ".jpeg")
            {
                return "image/jpeg";
            }

            if (ext == ".webp")
            {
                return "image/webp";
            }

            return "application/octet-stream";
        }

        private void ReplaceNullStrings(object obj)
            {
                if (obj == null)
                {
                    return;
                }

                var properties = obj.GetType().GetProperties();

                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        var value = (string)prop.GetValue(obj);

                        if (value == null)
                        {
                            prop.SetValue(obj, "");
                        }
                    }
                }
            }

        public async Task DownloadScreenshotAsync(int screenshotId, string targetPath, CancellationToken ct = default)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                cts.CancelAfter(TimeSpan.FromSeconds(RommConstants.UploadTimeoutSeconds));

                using (var response = await _http.GetAsync($"/api/screenshots/{screenshotId}/content", cts.Token))
                {
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(targetPath, bytes);
                }
            }
        }
    }
}
