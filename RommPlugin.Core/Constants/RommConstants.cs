namespace RommPlugin.Core.Constants
{
    public static class RommConstants
    {
        public const string PlatformPrefix = "RomM | ";
        public const string PlaylistPrefix = "RomM _ ";
        public const string RootCategoryName = "RomM";
        public const string InstalledGamesPlaylistName = "RomM _ Installed Games";
        public const string BackupFolderName = "RomM_Backups";
        public const string RomsSubfolder = "romm";

        public const string ImageTypeBoxFront = "Box - Front";
        public const string ImageTypeFanartBoxFront = "Fanart - Box - Front";
        public const string ImageTypeAdvertisementFlyerFront = "Advertisement Flyer - Front";
        public const string ImageTypeScreenshot = "Screenshot";

        public const string DownloadQueueFile = "download-queue.json";
        public const string DownloadStateFile = "download-state.json";
        public const string InstalledGamesFile = "installed-games.json";
        public const string SyncInformationFile = "sync_information.json";
        public const string PendingHierarchyFile = "pending_hierarchy.json";
        public const string PlatformsFile = "Platforms.xml";
        public const string LaunchboxConfigFile = "_launchbox.json";
        public const string CliExecutable = "RommPlugin.CLI.exe";

        public const string DeviceId = "launchbox";

        public const int HttpBufferSize = 8192;
        public const int MaxXmlBackups = 5;
        public const int MaxConcurrentDownloads = 5;

        public const int HttpTimeoutSeconds = 120;
        public const int UploadTimeoutSeconds = 300;
        public const int ApiPageSize = 1000;
        public const int MaxRetryAttempts = 5;
        public const int RetryBaseDelayMs = 1000;
    }
}
