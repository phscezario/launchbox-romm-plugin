using System.Collections.Generic;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for launching the external hierarchy CLI tool that rebuilds LaunchBox platform playlists and categories.
    /// </summary>
    public interface IRommHierarchyCli
    {
        /// <summary>
        /// Launches the hierarchy CLI tool to rebuild LaunchBox playlist XML files based on the current RomM platforms and games.
        /// </summary>
        /// <param name="platforms">The list of all RomM-sourced LaunchBox platforms.</param>
        /// <param name="rommGamesOnly">The list of all games belonging to RomM-sourced platforms.</param>
        /// <param name="platformCategoryMap">A mapping of platform names to their parent category names.</param>
        /// <param name="restartLaunchBox">If true, LaunchBox will be restarted after the CLI completes.</param>
        void LaunchHierarchyCli(List<IPlatform> platforms, List<IGame> rommGamesOnly, Dictionary<string, string> platformCategoryMap, bool restartLaunchBox = false);
    }
}
