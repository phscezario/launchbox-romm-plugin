using System;
using System.Threading.Tasks;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public interface IRommStatsService
    {
        Task<RommStats> FetchLatestStatsFromRomm(int romId);
        void CompareAndUpdateStats(IGame game, RommStats rommStats);
        Task SendPlaySessionToRomm(int rommGameId, DateTime startTime, DateTime endTime, long durationMs);
        Task SyncStatsOnGameLaunch(IGame game, int rommId);
        Task SyncStatsOnGameExit(IGame game, int rommId, DateTime startTime);
    }
}
