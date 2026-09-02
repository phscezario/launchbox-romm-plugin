using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Tracks which games have been installed locally from the RomM server.
    /// </summary>
    public interface IInstalledGamesService
    {
        /// <summary>
        /// Gets all installed game records, including those previously uninstalled.
        /// </summary>
        /// <returns>A read-only list of all tracked installed game records.</returns>
        IReadOnlyList<InstalledGameRecord> GetAll();

        /// <summary>
        /// Gets the installed game record for a specific RomM game ID.
        /// </summary>
        /// <param name="rommGameId">The RomM game ID to look up.</param>
        /// <returns>The matching <see cref="InstalledGameRecord"/>, or <c>null</c> if not found.</returns>
        InstalledGameRecord GetByGameId(int rommGameId);

        /// <summary>
        /// Determines whether a game is currently installed (marked as installed and not uninstalled).
        /// </summary>
        /// <param name="rommGameId">The RomM game ID to check.</param>
        /// <returns><c>true</c> if the game is installed; otherwise, <c>false</c>.</returns>
        bool IsInstalled(int rommGameId);

        /// <summary>
        /// Records a game as installed, or updates an existing record if one already exists.
        /// </summary>
        /// <param name="record">The installed game record to save.</param>
        void MarkInstalled(InstalledGameRecord record);

        /// <summary>
        /// Marks a game as uninstalled by recording the uninstallation timestamp.
        /// </summary>
        /// <param name="rommGameId">The RomM game ID to mark as uninstalled.</param>
        void MarkUninstalled(int rommGameId);

        /// <summary>
        /// Permanently removes all records of games that have been marked as uninstalled.
        /// </summary>
        void RemoveUninstalled();
    }
}
