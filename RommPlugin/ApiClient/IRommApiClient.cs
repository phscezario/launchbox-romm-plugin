using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RommPlugin.Core.Models;

namespace RommPlugin.ApiClient
{
    public interface IRommApiClient : IDisposable
    {
        void SetBasicAuthentication(string username, string password);
        void SetBearerAuthentication(string token);
        void ApplyAuthentication(RommPluginSettings settings);
        Task<List<RommPlatform>> GetPlatformsAsync();
        Task<List<RommGame>> GetAllGamesByPlatformAsync(int platformId);
        Task<RommGame> GetGameByIdAsync(int gameId);
        Task UpdateGameById(int gameId, RommUpdateGameRequest request);
        Task RemoveGameMetadataById(int gameId);
        Task<byte[]> DownloadBytesAsync(string url);
        Task<int> UploadScreenshotAsync(int gameId, string filePath);
        Task SetScreenshotPublicAsync(int screenshotId);
        Task<List<PlaySessionSchema>> GetPlaySessionsAsync(int romId);
        Task<PlaySessionIngestResponse> IngestPlaySessionsAsync(PlaySessionIngestPayload payload);
        Task UpdateGameLastPlayedAsync(int gameId);
        Task DownloadScreenshotAsync(int screenshotId, string targetPath, CancellationToken ct = default);
    }
}
