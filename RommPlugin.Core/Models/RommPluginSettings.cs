using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class RommPluginSettings
    {
        public string RommBaseUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string ClientApiToken { get; set; }

        public string RomsPath { get; set; }

        public bool KeepLocalData { get; set; }

        public bool SaveLogs { get; set; }

        public bool ProcessPendingOnStartup { get; set; } = true;

        public string Language { get; set; } = "en";

        public bool ForceFullResync { get; set; }

        public bool ForcePushToServer { get; set; }

        public DateTime? LastAutoSyncAt { get; set; }

        public int LogRetentionDays { get; set; } = 7;

        public bool PublicScreenshots { get; set; } = true;

        public bool UpdateStatsOnGameLaunch { get; set; } = false;

        public bool IsAdmin { get; set; } = false;

        public bool AutoUpdateEnabled { get; set; } = true;

        public int AutoSyncIntervalDays { get; set; } = 0;

        public int SaveBatchSize { get; set; } = 50;

        public List<int> LastSelectedPlatformIds { get; set; } = new List<int>();
    }
}
