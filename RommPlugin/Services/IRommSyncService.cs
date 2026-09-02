using System.Threading.Tasks;
using RommPlugin.ApiClient;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for synchronizing game metadata and media between LaunchBox and a RomM server.
    /// </summary>
    public interface IRommSyncService
    {
        /// <summary>
        /// Gets the RomM API client used for server communication.
        /// </summary>
        IRommApiClient Api { get; }

        /// <summary>
        /// Sets the RomM API client instance used for server communication.
        /// </summary>
        /// <param name="api">The API client to use for RomM server requests.</param>
        void SetApi(IRommApiClient api);

        /// <summary>
        /// Performs a bidirectional sync of platforms and games between LaunchBox and the RomM server.
        /// </summary>
        /// <param name="headless">If true, runs without user interaction using previously selected platforms.</param>
        /// <returns>A task representing the asynchronous sync operation.</returns>
        Task SyncAsync(bool headless = false);

        /// <summary>
        /// Pushes local LaunchBox metadata to the RomM server for all games in selected platforms.
        /// </summary>
        /// <param name="username">The RomM server username for basic authentication.</param>
        /// <param name="password">The RomM server password for basic authentication.</param>
        /// <param name="clientApiToken">Optional bearer token for authentication. If provided, username and password are ignored.</param>
        /// <returns>A task representing the asynchronous update operation.</returns>
        Task UpdateServerMetadata(string username, string password, string clientApiToken = null);

        /// <summary>
        /// Applies metadata from a RomM server game to a local LaunchBox game based on the current settings.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="rommGame">The RomM server game containing the metadata to apply.</param>
        /// <param name="settings">The plugin settings controlling overwrite behavior.</param>
        void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings);

        /// <summary>
        /// Pushes local LaunchBox game metadata to the RomM server and syncs screenshots.
        /// </summary>
        /// <param name="game">The local LaunchBox game whose metadata to push.</param>
        /// <param name="remoteGame">The corresponding RomM server game.</param>
        /// <param name="settings">The plugin settings.</param>
        /// <returns>A task representing the asynchronous push operation.</returns>
        Task PushGameMetadataAsync(IGame game, RommGame remoteGame, RommPluginSettings settings);

        /// <summary>
        /// Synchronizes screenshots bidirectionally between a local LaunchBox game and the RomM server.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <param name="remoteGame">The corresponding RomM server game.</param>
        /// <param name="settings">The plugin settings.</param>
        /// <returns>A task representing the asynchronous sync operation.</returns>
        Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings);
    }
}
