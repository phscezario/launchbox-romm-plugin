using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents all configurable settings for the LaunchBox RomM Plugin.
    /// Persisted to <c>settings.json</c> in the plugin folder.
    /// </summary>
    public class RommPluginSettings
    {
        /// <summary>The base URL of the RomM server (e.g., "http://192.168.1.100:9000").</summary>
        public string RommBaseUrl { get; set; }

        /// <summary>The RomM username for basic authentication.</summary>
        public string Username { get; set; }

        /// <summary>The RomM password for basic authentication (encrypted at rest via DPAPI).</summary>
        public string Password { get; set; }

        /// <summary>The RomM Client API token (rmm_...). Takes priority over username/password when set.</summary>
        public string ClientApiToken { get; set; }

        /// <summary>The local folder path where ROMs will be installed.</summary>
        public string RomsPath { get; set; }

        /// <summary>When true, only fills empty/null fields during sync; when false, overwrites all fields with server data.</summary>
        public bool KeepLocalData { get; set; }

        /// <summary>When true, enables file-based logging to the Logs folder.</summary>
        public bool SaveLogs { get; set; }

        /// <summary>When true, processes pending install/uninstall events on LaunchBox startup.</summary>
        public bool ProcessPendingOnStartup { get; set; } = true;

        /// <summary>The UI language code (e.g., "en" or "pt-BR").</summary>
        public string Language { get; set; } = "en";

        /// <summary>When true, clears resume state and reprocesses all platforms on the next sync.</summary>
        public bool ForceFullResync { get; set; }

        /// <summary>When true (admin only), pushes all local metadata, artwork, and screenshots to the server.</summary>
        public bool ForcePushToServer { get; set; }

        /// <summary>The timestamp of the last auto-sync, or null if never auto-synced.</summary>
        public DateTime? LastAutoSyncAt { get; set; }

        /// <summary>The number of days to retain log files before automatic deletion.</summary>
        public int LogRetentionDays { get; set; } = 7;

        /// <summary>When true, uploaded screenshots are visible to all RomM users.</summary>
        public bool PublicScreenshots { get; set; } = true;

        /// <summary>When true, syncs play count and play time on game launch/exit.</summary>
        public bool UpdateStatsOnGameLaunch { get; set; } = false;

        /// <summary>When true, enables bidirectional sync (pull from RomM + push local metadata to server).</summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>When true, checks for GitHub updates on startup.</summary>
        public bool AutoUpdateEnabled { get; set; } = true;

        /// <summary>Auto-sync interval in days: -1 = disabled, 0 = every startup, N = every N days.</summary>
        public int AutoSyncIntervalDays { get; set; } = 0;

        /// <summary>The number of games to process per save batch during sync.</summary>
        public int SaveBatchSize { get; set; } = 50;

        /// <summary>List of platform IDs selected by the user in the last sync session.</summary>
        public List<int> LastSelectedPlatformIds { get; set; } = new List<int>();
    }
}
