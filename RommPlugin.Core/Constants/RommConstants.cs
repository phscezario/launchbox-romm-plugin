namespace RommPlugin.Core.Constants
{
    /// <summary>
    /// Contains all constants used across the LaunchBox RomM Plugin.
    /// Includes prefixes, filenames, image types, and configuration values.
    /// </summary>
    public static class RommConstants
    {
        /// <summary>Prefix added to RomM platform names in LaunchBox (e.g., "RomM | Nintendo 64").</summary>
        public const string PlatformPrefix = "RomM | ";

        /// <summary>Prefix added to RomM playlist names in LaunchBox (e.g., "RomM _ Installed Games").</summary>
        public const string PlaylistPrefix = "RomM _ ";

        /// <summary>Root category name for all RomM platforms in LaunchBox hierarchy.</summary>
        public const string RootCategoryName = "RomM";

        /// <summary>Name of the playlist containing all installed RomM games.</summary>
        public const string InstalledGamesPlaylistName = "RomM _ Installed Games";

        /// <summary>Folder name for XML backups created before sync operations.</summary>
        public const string BackupFolderName = "RomM_Backups";

        /// <summary>Subfolder name under the ROMs path where game files are stored.</summary>
        public const string RomsSubfolder = "romm";

        /// <summary>Image type identifier for front box art.</summary>
        public const string ImageTypeBoxFront = "Box - Front";

        /// <summary>Image type identifier for fan art box front images.</summary>
        public const string ImageTypeFanartBoxFront = "Fanart - Box - Front";

        /// <summary>Image type identifier for advertisement flyer images.</summary>
        public const string ImageTypeAdvertisementFlyerFront = "Advertisement Flyer - Front";

        /// <summary>Image type identifier for game screenshots.</summary>
        public const string ImageTypeScreenshot = "Screenshot";

        /// <summary>Filename for the download queue persistence file.</summary>
        public const string DownloadQueueFile = "download-queue.json";

        /// <summary>Filename for the download state persistence file.</summary>
        public const string DownloadStateFile = "download-state.json";

        /// <summary>Filename for the installed games persistence file.</summary>
        public const string InstalledGamesFile = "installed-games.json";

        /// <summary>Filename for the sync resume state persistence file.</summary>
        public const string SyncInformationFile = "sync_information.json";

        /// <summary>Filename for pending Parents.xml hierarchy fixes.</summary>
        public const string PendingHierarchyFile = "pending_hierarchy.json";

        /// <summary>LaunchBox Platforms.xml filename.</summary>
        public const string PlatformsFile = "Platforms.xml";

        /// <summary>Per-game configuration filename for advanced LaunchBox integration.</summary>
        public const string LaunchboxConfigFile = "_launchbox.json";

        /// <summary>Filename of the CLI executable used for hierarchy fixes.</summary>
        public const string CliExecutable = "RommPlugin.CLI.exe";

        /// <summary>Device identifier sent with play session data to RomM.</summary>
        public const string DeviceId = "launchbox";

        /// <summary>Buffer size in bytes for HTTP stream reading.</summary>
        public const int HttpBufferSize = 8192;

        /// <summary>Maximum number of XML backup files to retain.</summary>
        public const int MaxXmlBackups = 5;

        /// <summary>Maximum number of concurrent downloads in the download queue.</summary>
        public const int MaxConcurrentDownloads = 5;

        /// <summary>HTTP request timeout in seconds for standard API calls.</summary>
        public const int HttpTimeoutSeconds = 120;

        /// <summary>HTTP timeout in seconds for file upload operations.</summary>
        public const int UploadTimeoutSeconds = 300;

        /// <summary>Number of games per page when fetching from the RomM API.</summary>
        public const int ApiPageSize = 1000;

        /// <summary>Maximum number of retry attempts for failed HTTP requests.</summary>
        public const int MaxRetryAttempts = 5;

        /// <summary>Base delay in milliseconds between retry attempts (exponential backoff).</summary>
        public const int RetryBaseDelayMs = 1000;
    }
}
