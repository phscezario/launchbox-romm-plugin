using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RommPlugin.ApiClient;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Synchronizes play statistics between LaunchBox games and the RomM server.
    /// Handles fetching stats, comparing them with local data, and sending play sessions.
    /// </summary>
    public class RommStatsService : IRommStatsService
    {
        private readonly IRommApiClient _api;

        /// <summary>
        /// Initializes a new instance of the <see cref="RommStatsService"/> class.
        /// </summary>
        /// <param name="api">The RomM API client used for fetching and sending play statistics.</param>
        public RommStatsService(IRommApiClient api)
        {
            _api = api;
        }

        /// <inheritdoc/>
        public async Task<RommStats> FetchLatestStatsFromRomm(int romId)
        {
            try
            {
                var sessions = await _api.GetPlaySessionsAsync(romId);

                if (sessions == null || sessions.Count == 0)
                {
                    return new RommStats();
                }

                return new RommStats
                {
                    PlayCount = sessions.Count,
                    TotalPlayTimeMs = sessions.Sum(s => s.DurationMs),
                    LastPlayed = sessions.Max(s => s.EndTime)
                };
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error fetching stats from RomM for rom {romId}: {ex.Message}");
                return new RommStats();
            }
        }

        /// <inheritdoc/>
        public void CompareAndUpdateStats(IGame game, RommStats rommStats)
        {
            if (rommStats.LastPlayed == null)
            {
                return;
            }

            if (game.LastPlayedDate == null || rommStats.LastPlayed > game.LastPlayedDate)
            {
                game.PlayCount = rommStats.PlayCount;
                game.PlayTime = rommStats.TotalPlayTimeSeconds;
                game.LastPlayedDate = rommStats.LastPlayed;
                RommLogger.Log($"Updated stats for '{game.Title}': PlayCount={rommStats.PlayCount}, PlayTime={rommStats.TotalPlayTimeSeconds}s, LastPlayed={rommStats.LastPlayed}");
            }
        }

        /// <inheritdoc/>
        public async Task SendPlaySessionToRomm(int rommGameId, DateTime startTime, DateTime endTime, long durationMs)
        {
            try
            {
                var payload = new PlaySessionIngestPayload
                {
                    DeviceId = RommConstants.DeviceId,
                    Sessions = new List<PlaySessionEntry>
                    {
                        new PlaySessionEntry
                        {
                            RomId = rommGameId,
                            StartTime = startTime.ToString("o"),
                            EndTime = endTime.ToString("o"),
                            DurationMs = durationMs
                        }
                    }
                };

                await _api.IngestPlaySessionsAsync(payload);
                await _api.UpdateGameLastPlayedAsync(rommGameId);
                RommLogger.Log($"Sent play session to RomM: romId={rommGameId}, duration={durationMs}ms");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error sending play session to RomM for rom {rommGameId}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task SyncStatsOnGameLaunch(IGame game, int rommId)
        {
            try
            {
                var rommStats = await FetchLatestStatsFromRomm(rommId);
                CompareAndUpdateStats(game, rommStats);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error syncing stats on game launch: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task SyncStatsOnGameExit(IGame game, int rommId, DateTime startTime)
        {
            try
            {
                var durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                await SendPlaySessionToRomm(rommId, startTime, DateTime.UtcNow, durationMs);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error syncing stats on game exit: {ex.Message}");
            }
        }
    }
}
