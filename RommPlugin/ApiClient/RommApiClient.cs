using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Models;

namespace RommPlugin.ApiClient
{
    public class RommApiClient
    {
        private readonly HttpClient _http;
        private string _token;

        public RommApiClient(string baseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task LoginAsync(string username, string password)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "scope", "platforms.read roms.read" },
                { "username", username },
                { "password", password },
                { "client_id", "" },
                { "client_secret", "" },
                { "refresh_token", "" }
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await _http.PostAsync("/api/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var auth = JsonConvert.DeserializeObject<RommAuthResponse>(json);

            _token = auth.AccessToken;

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", _token);
        }

        public async Task<List<RommPlatform>> GetPlatformsAsync()
        {
            EnsureAuthenticated();

            var response = await _http.GetAsync("/api/platforms");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RommPlatform>>(json);
        }

        public async Task<List<RommGame>> GetAllGamesByPlatformAsync(int platformId)
        {
            EnsureAuthenticated();

            var allGames = new List<RommGame>();
            int limit = 100;
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

        public async Task DownloadGameAsync(int gameId, string destinationFile)
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
            }
        }

        private void EnsureAuthenticated()
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException("RomM API not authenticated");
        }
    }
}
