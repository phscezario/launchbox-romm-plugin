namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Contains the names of all custom fields stored on LaunchBox games by the RomM plugin.
    /// These fields track synchronization state, metadata hashes, and RomM-specific identifiers.
    /// </summary>
    public static class GameCustomFields
    {
        /// <summary>Custom field storing the RomM game ID.</summary>
        public const string GameId = "romm_game_id";

        /// <summary>Custom field storing the RomM platform ID.</summary>
        public const string PlatformId = "romm_platform_id";

        /// <summary>Custom field storing the ROM file path on the RomM server.</summary>
        public const string RemotePath = "romm_remote_path";

        /// <summary>Custom field storing the ROM filename.</summary>
        public const string FileName = "romm_file_name";

        /// <summary>Custom field indicating whether the game is a folder-based game.</summary>
        public const string IsFolderGame = "romm_isFolder_game";

        /// <summary>Custom field storing the timestamp of the last successful sync.</summary>
        public const string LastSyncedAt = "romm_last_synced_at";

        /// <summary>Custom field storing the hash of local metadata for change detection.</summary>
        public const string LocalMetadataHash = "romm_local_metadata_hash";

        /// <summary>Custom field storing the hash of remote metadata for change detection.</summary>
        public const string RemoteMetadataHash = "romm_remote_metadata_hash";

        /// <summary>Custom field storing the timestamp of the last auto-sync for this game.</summary>
        public const string LastAutoSyncAt = "romm_last_auto_sync_at";

        /// <summary>Custom field storing the IGDB rating value.</summary>
        public const string IgdbRating = "romm_igdb_rating";

        /// <summary>Custom field storing IGDB collections.</summary>
        public const string IgdbCollections = "romm_igdb_collections";

        /// <summary>Custom field storing IGDB franchises.</summary>
        public const string IgdbFranchises = "romm_igdb_franchises";

        /// <summary>Custom field storing IGDB game modes.</summary>
        public const string IgdbGameModes = "romm_igdb_game_modes";

        /// <summary>Custom field storing IGDB platform references.</summary>
        public const string IgdbPlatforms = "romm_igdb_platforms";

        /// <summary>Custom field storing IGDB similar games.</summary>
        public const string IgdbSimilarGames = "romm_igdb_similar_games";

        /// <summary>Custom field storing the ScreenScraper score.</summary>
        public const string SsScore = "romm_ss_score";

        /// <summary>Custom field storing ScreenScraper language information.</summary>
        public const string SsLanguages = "romm_ss_languages";

        /// <summary>Custom field storing ScreenScraper region information.</summary>
        public const string SsRegion = "romm_ss_region";

        /// <summary>Custom field storing the ScreenScraper synopsis.</summary>
        public const string SsSynopsis = "romm_ss_synopsis";

        /// <summary>Custom field storing game franchises from RomM metadata.</summary>
        public const string Franchises = "romm_franchises";

        /// <summary>Custom field storing game modes from RomM metadata.</summary>
        public const string GameModes = "romm_game_modes";

        /// <summary>Custom field storing age ratings from RomM metadata.</summary>
        public const string AgeRatings = "romm_age_ratings";

        /// <summary>Custom field storing the supported player count.</summary>
        public const string PlayerCount = "romm_player_count";

        /// <summary>Custom field storing the average community rating.</summary>
        public const string AverageRating = "romm_average_rating";
    }
}
