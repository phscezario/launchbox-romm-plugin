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
        private string _token;

        public RommApiClient(string baseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public void SetBasicAuthentication(string username, string password)
        {
            var credentials = $"{username}:{password}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", base64);
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

        public async Task UpdateGameById(int gameId, RommUpdateGameRequest request)
        {
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(request.Name ?? ""), "name");
            content.Add(new StringContent(request.FsName ?? ""), "fs_name");
            content.Add(new StringContent(request.Summary ?? ""), "summary");
            content.Add(new StringContent(request.LaunchboxId?.ToString() ?? ""), "launchbox_id");

            var launchboxJson = JsonConvert.SerializeObject(request.RawLaunchboxMetadata);
            content.Add(new StringContent(launchboxJson, Encoding.UTF8, "application/json"), "raw_launchbox_metadata");

            var manualJson = JsonConvert.SerializeObject(request.RawManualMetadata ?? new { });
            content.Add(new StringContent(manualJson, Encoding.UTF8, "application/json"), "raw_manual_metadata");

            if (!string.IsNullOrEmpty(request.ArtworkPath) && File.Exists(request.ArtworkPath))
            {
                var fileStream = File.OpenRead(request.ArtworkPath);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                content.Add(fileContent, "artwork", Path.GetFileName(request.ArtworkPath));
            }

            var response = await _http.PutAsync($"api/roms/{gameId}?remove_cover=false&unmatch_metadata=false", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveGameMetadataById(int gameId)
        {
            var response = await _http.PutAsync($"api/roms/{gameId}?remove_cover=false&unmatch_metadata=true", null);
            
            response.EnsureSuccessStatusCode();
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
    }
}
