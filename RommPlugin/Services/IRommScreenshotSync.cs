using System.Threading.Tasks;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for synchronizing screenshots and cover art between LaunchBox games and the RomM server.
    /// </summary>
    public interface IRommScreenshotSync
    {
        /// <summary>
        /// Synchronizes screenshots bidirectionally between a local LaunchBox game and the RomM server.
        /// Uploads local screenshots not on the server and downloads remote screenshots not stored locally.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <param name="remoteGame">The corresponding RomM server game.</param>
        /// <param name="settings">The plugin settings.</param>
        /// <returns>A task representing the asynchronous sync operation.</returns>
        Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings);

        /// <summary>
        /// Downloads cover art from the RomM server and assigns it as the Box Front image for a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to set cover art for.</param>
        /// <param name="rommGame">The RomM server game containing the cover art URL.</param>
        /// <returns>A task representing the asynchronous download and assignment operation.</returns>
        Task DownloadAndSetCoverArt(IGame game, RommGame rommGame);

        /// <summary>
        /// Gets the file path of the existing cover art image for a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <returns>The file path of the cover art image, or an empty string if not found.</returns>
        string GetCoverImagePath(IGame game);

        /// <summary>
        /// Determines whether the game has any box front type image (Box Front, Fanart Box Front, or Advertisement Flyer Front).
        /// </summary>
        /// <param name="game">The local LaunchBox game to check.</param>
        /// <returns>True if at least one box front type image exists; otherwise false.</returns>
        bool HasAnyBoxFrontImage(IGame game);

        /// <summary>
        /// Deletes all images associated with a LaunchBox game from the local filesystem.
        /// </summary>
        /// <param name="game">The local LaunchBox game whose images should be deleted.</param>
        void DeleteGameImages(IGame game);
    }
}
