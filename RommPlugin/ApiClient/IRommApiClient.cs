using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RommPlugin.Core.Models;

namespace RommPlugin.ApiClient
{
    /// <summary>
    /// Defines the contract for communicating with a Romm server.
    /// </summary>
    public interface IRommApiClient : IDisposable
    {
        /// <summary>
        /// Configures the client to use HTTP Basic authentication.
        /// </summary>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        void SetBasicAuthentication(string username, string password);

        /// <summary>
        /// Configures the client to use Bearer token authentication.
        /// </summary>
        /// <param name="token">The bearer token to include in the Authorization header.</param>
        void SetBearerAuthentication(string token);

        /// <summary>
        /// Applies authentication settings from the given plugin settings.
        /// Uses the client API token if present, otherwise falls back to username/password.
        /// </summary>
        /// <param name="settings">The plugin settings containing credentials.</param>
        void ApplyAuthentication(RommPluginSettings settings);

        /// <summary>
        /// Retrieves all platforms from the Romm server.
        /// </summary>
        /// <returns>A list of available platforms.</returns>
        Task<List<RommPlatform>> GetPlatformsAsync();

        /// <summary>
        /// Retrieves all games for the specified platform, handling pagination automatically.
        /// </summary>
        /// <param name="platformId">The unique identifier of the platform.</param>
        /// <returns>A list of all games belonging to the platform.</returns>
        Task<List<RommGame>> GetAllGamesByPlatformAsync(int platformId);

        /// <summary>
        /// Retrieves a single game by its unique identifier.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game.</param>
        /// <returns>The game matching the specified identifier.</returns>
        Task<RommGame> GetGameByIdAsync(int gameId);

        /// <summary>
        /// Updates a game's metadata and optionally its artwork on the Romm server.
        /// Retries on transient failures and server errors.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game to update.</param>
        /// <param name="request">The update request containing the new metadata and artwork path.</param>
        Task UpdateGameById(int gameId, RommUpdateGameRequest request);

        /// <summary>
        /// Removes matched metadata from the specified game, resetting it to unmatched state.
        /// Retries on transient failures and server errors.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game whose metadata should be removed.</param>
        Task RemoveGameMetadataById(int gameId);

        /// <summary>
        /// Downloads raw bytes from the specified URL.
        /// Handles both absolute and relative URLs.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <returns>The downloaded bytes.</returns>
        Task<byte[]> DownloadBytesAsync(string url);

        /// <summary>
        /// Uploads a screenshot file for the specified game.
        /// Retries on transient failures.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game to associate the screenshot with.</param>
        /// <param name="filePath">The local path to the screenshot file.</param>
        /// <returns>The unique identifier of the uploaded screenshot.</returns>
        Task<int> UploadScreenshotAsync(int gameId, string filePath);

        /// <summary>
        /// Marks a screenshot as public, making it visible to other users.
        /// Retries on transient failures.
        /// </summary>
        /// <param name="screenshotId">The unique identifier of the screenshot to make public.</param>
        Task SetScreenshotPublicAsync(int screenshotId);

        /// <summary>
        /// Retrieves play session records for the specified ROM.
        /// Retries on transient failures.
        /// </summary>
        /// <param name="romId">The unique identifier of the ROM.</param>
        /// <returns>A list of play session records associated with the ROM.</returns>
        Task<List<PlaySessionSchema>> GetPlaySessionsAsync(int romId);

        /// <summary>
        /// Submits play session data to the Romm server for ingestion.
        /// Retries on transient failures.
        /// </summary>
        /// <param name="payload">The play session payload to ingest.</param>
        /// <returns>The ingestion response from the server.</returns>
        Task<PlaySessionIngestResponse> IngestPlaySessionsAsync(PlaySessionIngestPayload payload);

        /// <summary>
        /// Updates the last-played timestamp for the specified game on the Romm server.
        /// Retries on transient failures.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game to update.</param>
        Task UpdateGameLastPlayedAsync(int gameId);

        /// <summary>
        /// Downloads a screenshot to a local file path.
        /// </summary>
        /// <param name="screenshotId">The unique identifier of the screenshot to download.</param>
        /// <param name="targetPath">The local file path to write the screenshot to.</param>
        /// <param name="ct">An optional cancellation token.</param>
        Task DownloadScreenshotAsync(int screenshotId, string targetPath, CancellationToken ct = default);
    }
}
