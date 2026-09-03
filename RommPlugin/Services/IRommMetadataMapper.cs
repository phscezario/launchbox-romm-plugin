using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for mapping metadata from RomM server game objects to LaunchBox game properties.
    /// </summary>
    public interface IRommMetadataMapper
    {
        /// <summary>
        /// Applies all available metadata from a RomM server game to a local LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="rommGame">The RomM server game containing source metadata.</param>
        /// <param name="settings">The plugin settings controlling overwrite behavior.</param>
        void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings);

        /// <summary>
        /// Applies release date metadata from multiple sources to a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="lb">LaunchBox database metadata.</param>
        /// <param name="ss">ScreenScraper metadata.</param>
        /// <param name="igdb">IGDB metadata.</param>
        /// <param name="meta">RomM generic metadata.</param>
        /// <param name="overwrite">If true, overwrites existing values; otherwise only fills empty fields.</param>
        void ApplyReleaseDate(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, IgdbMetadata igdb, RommGameMeta meta, bool overwrite);

        /// <summary>
        /// Applies maximum player count metadata from multiple sources to a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="lb">LaunchBox database metadata.</param>
        /// <param name="ss">ScreenScraper metadata.</param>
        /// <param name="overwrite">If true, overwrites existing values; otherwise only fills empty fields.</param>
        void ApplyMaxPlayers(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, bool overwrite);

        /// <summary>
        /// Applies play mode (e.g., Cooperative) metadata to a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="lb">LaunchBox database metadata.</param>
        /// <param name="overwrite">If true, overwrites existing values; otherwise only fills empty fields.</param>
        void ApplyPlayMode(IGame game, LaunchBoxMetadataModel lb, bool overwrite);

        /// <summary>
        /// Applies video URL metadata from multiple sources to a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="lb">LaunchBox database metadata.</param>
        /// <param name="igdb">IGDB metadata.</param>
        /// <param name="overwrite">If true, overwrites existing values; otherwise only fills empty fields.</param>
        void ApplyVideoUrl(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, bool overwrite);

        /// <summary>
        /// Applies community rating metadata from multiple sources to a LaunchBox game.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="lb">LaunchBox database metadata.</param>
        /// <param name="igdb">IGDB metadata.</param>
        /// <param name="meta">RomM generic metadata.</param>
        /// <param name="overwrite">If true, overwrites existing values; otherwise only fills empty fields.</param>
        void ApplyCommunityRating(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, RommGameMeta meta, bool overwrite);
    }
}
