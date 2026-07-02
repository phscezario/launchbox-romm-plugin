using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Models;

namespace RommPlugin.ApiClient
{
    public class RommApiClient
    {
        private readonly HttpClient _http;

        public RommApiClient(string baseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = System.Threading.Timeout.InfiniteTimeSpan
            };
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
            var response = await _http.GetAsync("/api/platforms");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RommPlatform>>(json);
        }

        public async Task<List<RommGame>> GetAllGamesByPlatformAsync(int platformId)
        {
            var allGames = new List<RommGame>();
            int limit = 1000;
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var url = $"/api/roms?" +
                          $"with_filter_values=true&" +
                          $"platform_ids={platformId}&" +
                          $"order_by=name&order_dir=asc&" +
                          $"limit={limit}&offset={offset}";

                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var rommResponse = JsonConvert.DeserializeObject<RommGameResponse>(json);

                if (rommResponse?.Items != null && rommResponse.Items.Count > 0)
                {
                    allGames.AddRange(rommResponse.Items);
                    offset += rommResponse.Items.Count;
                }
                else
                {
                    hasMore = false;
                }
            }

            return allGames;
        }

        public async Task<RommGame> GetGameByIdAsync(int gameId)
        {
            var response = await _http.GetAsync($"/api/roms/{gameId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RommGame>(json);
        }

        public async Task UpdateGameById(int gameId, RommUpdateGameRequest request)
        {
            const int maxAttempts = 5;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var content = new MultipartFormDataContent();

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
                            Path.GetFileName(request.ArtworkPath)
                        );

                        var response = await _http.PutAsync(
                            $"api/roms/{gameId}?remove_cover=false&unmatch_metadata=false",
                            content
                        );

                        if (!response.IsSuccessStatusCode)
                        {
                            if ((int)response.StatusCode >= 500)
                            {
                                if (attempt == maxAttempts)
                                {
                                    throw new HttpRequestException($"Server error {(int)response.StatusCode}");
                                }

                                await Task.Delay(500 * attempt);
                                continue;
                            }

                            response.EnsureSuccessStatusCode();
                        }

                        return;
                    }
                }
                else
                {
                    var response = await _http.PutAsync(
                        $"api/roms/{gameId}?remove_cover=false&unmatch_metadata=false",
                        content
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        if ((int)response.StatusCode >= 500)
                        {
                            if (attempt == maxAttempts)
                            {
                                throw new HttpRequestException($"Server error {(int)response.StatusCode}");
                            }

                            await Task.Delay(500 * attempt);
                            continue;
                        }

                        response.EnsureSuccessStatusCode();
                    }

                    return;
                }
            }
        }

        public async Task RemoveGameMetadataById(int gameId)
        {
            var response = await _http.PutAsync($"api/roms/{gameId}?remove_cover=false&unmatch_metadata=true", null);

            response.EnsureSuccessStatusCode();
            return;
        }

        public async Task DownloadGameAsync(int gameId, string destinationFile)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await _http.GetAsync(
                        $"/api/roms/download?rom_ids={gameId}",
                        HttpCompletionOption.ResponseHeadersRead
                    );

                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var file = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(file);
                        return;
                    }
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await Task.Delay(1000);
                }
            }
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
            url = url.Replace(" ", "%20");

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var response = await _http.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }

            var relativeResponse = await _http.GetAsync(url);
            relativeResponse.EnsureSuccessStatusCode();
            return await relativeResponse.Content.ReadAsByteArrayAsync();
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
    }
}
