using System;
using System.Threading.Tasks;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for synchronizing play statistics between LaunchBox games and the RomM server.
    /// </summary>
    public interface IRommStatsService
    {
        /// <summary>
        /// Fetches the latest play statistics for a game from the RomM server.
        /// </summary>
        /// <param name="romId">The RomM server game ID.</param>
        /// <returns>A task containing the aggregated play statistics from the server.</returns>
        Task<RommStats> FetchLatestStatsFromRomm(int romId);

        /// <summary>
        /// Compares remote stats with local game stats and updates the local game if the remote data is newer.
        /// </summary>
        /// <param name="game">The local LaunchBox game to update.</param>
        /// <param name="rommStats">The play statistics from the RomM server.</param>
        void CompareAndUpdateStats(IGame game, RommStats rommStats);

        /// <summary>
        /// Sends a completed play session to the RomM server for recording.
        /// </summary>
        /// <param name="rommGameId">The RomM server game ID.</param>
        /// <param name="startTime">The UTC time when the game session started.</param>
        /// <param name="endTime">The UTC time when the game session ended.</param>
        /// <param name="durationMs">The duration of the play session in milliseconds.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendPlaySessionToRomm(int rommGameId, DateTime startTime, DateTime endTime, long durationMs);

        /// <summary>
        /// Syncs the latest play statistics from the RomM server to the local LaunchBox game when a game is launched.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <param name="rommId">The RomM server game ID.</param>
        /// <returns>A task representing the asynchronous sync operation.</returns>
        Task SyncStatsOnGameLaunch(IGame game, int rommId);

        /// <summary>
        /// Sends the play session duration to the RomM server when a game is exited.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <param name="rommId">The RomM server game ID.</param>
        /// <param name="startTime">The UTC time when the game session started.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SyncStatsOnGameExit(IGame game, int rommId, DateTime startTime);
    }
}
