using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a ROM game from the RomM server.
    /// </summary>
    public class RommGame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RommGame"/> class with default values.
        /// </summary>
        public RommGame()
        {
            Name = "";
            PlatformSlug = "";
            PlatformFsSlug = "";
            PlatformDisplayName = "";
            FsName = "";
            FsNameNoTags = "";
            FsNameNoExt = "";
            FsExtension = "";
            FsPath = "";
            Slug = "";
            Summary = "";
            YoutubeVideoId = "";
            PathCoverSmall = "";
            PathCoverLarge = "";
            UrlCover = "";
            PathManual = "";
            UrlManual = "";

            AlternativeNames = new List<string>();
            Files = new List<RommFile>();
            MergedScreenshots = new List<string>();
        }

        /// <summary>
        /// Gets or sets the unique identifier of the game.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp of the game.
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp of the game.
        /// </summary>
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the display name of the game.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the LaunchBox identifier for the game.
        /// </summary>
        [JsonProperty("launchbox_id")]
        public int? LaunchboxId { get; set; }

        /// <summary>
        /// Gets or sets the platform identifier for the game.
        /// </summary>
        [JsonProperty("platform_id")]
        public int PlatformId { get; set; }

        /// <summary>
        /// Gets or sets the platform slug identifier.
        /// </summary>
        [JsonProperty("platform_slug")]
        public string PlatformSlug { get; set; }

        /// <summary>
        /// Gets or sets the platform filesystem slug.
        /// </summary>
        [JsonProperty("platform_fs_slug")]
        public string PlatformFsSlug { get; set; }

        /// <summary>
        /// Gets or sets the platform display name.
        /// </summary>
        [JsonProperty("platform_display_name")]
        public string PlatformDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the filesystem name of the ROM file.
        /// </summary>
        [JsonProperty("fs_name")]
        public string FsName { get; set; }

        /// <summary>
        /// Gets or sets the filesystem name without tags.
        /// </summary>
        [JsonProperty("fs_name_no_tags")]
        public string FsNameNoTags { get; set; }

        /// <summary>
        /// Gets or sets the filesystem name without extension.
        /// </summary>
        [JsonProperty("fs_name_no_ext")]
        public string FsNameNoExt { get; set; }

        /// <summary>
        /// Gets or sets the file extension of the ROM file.
        /// </summary>
        [JsonProperty("fs_extension")]
        public string FsExtension { get; set; }

        /// <summary>
        /// Gets or sets the filesystem path to the ROM file.
        /// </summary>
        [JsonProperty("fs_path")]
        public string FsPath { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonProperty("fs_size_bytes")]
        public long? FsSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the URL-friendly slug identifier.
        /// </summary>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the game summary or description.
        /// </summary>
        [JsonProperty("summary")]
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the list of alternative names for the game.
        /// </summary>
        [JsonProperty("alternative_names")]
        public List<string> AlternativeNames { get; set; }

        /// <summary>
        /// Gets or sets the YouTube video ID for the game trailer.
        /// </summary>
        [JsonProperty("youtube_video_id")]
        public string YoutubeVideoId { get; set; }

        /// <summary>
        /// Gets or sets the RomM game metadata.
        /// </summary>
        [JsonProperty("metadatum")]
        public RommGameMeta Metadatum { get; set; }

        /// <summary>
        /// Gets or sets the IGDB metadata for the game.
        /// </summary>
        [JsonProperty("igdb_metadata")]
        public IgdbMetadata IgdbMetadata { get; set; }

        /// <summary>
        /// Gets or sets the LaunchBox metadata for the game.
        /// </summary>
        [JsonProperty("launchbox_metadata")]
        public LaunchBoxMetadataModel LaunchBoxMetadata { get; set; }

        /// <summary>
        /// Gets or sets the Screenscraper metadata for the game.
        /// </summary>
        [JsonProperty("ss_metadata")]
        public SsMetadata SsMetadata { get; set; }

        /// <summary>
        /// Gets or sets the path to the small cover image.
        /// </summary>
        [JsonProperty("path_cover_small")]
        public string PathCoverSmall { get; set; }

        /// <summary>
        /// Gets or sets the path to the large cover image.
        /// </summary>
        [JsonProperty("path_cover_large")]
        public string PathCoverLarge { get; set; }

        /// <summary>
        /// Gets or sets the URL to the cover image.
        /// </summary>
        [JsonProperty("url_cover")]
        public string UrlCover { get; set; }

        /// <summary>
        /// Gets or sets whether the game has a manual.
        /// </summary>
        [JsonProperty("has_manual")]
        public bool HasManual { get; set; }

        /// <summary>
        /// Gets or sets the path to the game manual.
        /// </summary>
        [JsonProperty("path_manual")]
        public string PathManual { get; set; }

        /// <summary>
        /// Gets or sets the URL to the game manual.
        /// </summary>
        [JsonProperty("url_manual")]
        public string UrlManual { get; set; }

        /// <summary>
        /// Gets or sets whether the game has been identified.
        /// </summary>
        [JsonProperty("is_identified")]
        public bool IsIdentified { get; set; }

        /// <summary>
        /// Gets or sets whether the game is unidentified.
        /// </summary>
        [JsonProperty("is_unidentified")]
        public bool IsUnidentified { get; set; }

        /// <summary>
        /// Gets or sets whether the game file is missing from the filesystem.
        /// </summary>
        [JsonProperty("missing_from_fs")]
        public bool MissingFromFs { get; set; }

        /// <summary>
        /// Gets or sets whether the game has a simple single file structure.
        /// </summary>
        [JsonProperty("has_simple_single_file")]
        public bool HasSimpleSingleFile { get; set; }

        /// <summary>
        /// Gets or sets whether the game has a nested single file structure.
        /// </summary>
        [JsonProperty("has_nested_single_file")]
        public bool HasNestedSingleFile { get; set; }

        /// <summary>
        /// Gets or sets whether the game has multiple files.
        /// </summary>
        [JsonProperty("has_multiple_files")]
        public bool HasMultipleFiles { get; set; }

        /// <summary>
        /// Gets or sets the list of ROM files associated with the game.
        /// </summary>
        [JsonProperty("files")]
        public List<RommFile> Files { get; set; }

        /// <summary>
        /// Gets or sets the list of merged screenshot paths.
        /// </summary>
        [JsonProperty("merged_screenshots")]
        public List<string> MergedScreenshots { get; set; }

        /// <summary>
        /// Gets or sets the list of user-uploaded screenshots.
        /// </summary>
        [JsonProperty("user_screenshots")]
        public List<RommScreenshot> UserScreenshots { get; set; } = new List<RommScreenshot>();
    }

    /// <summary>
    /// Represents a user-uploaded screenshot for a game.
    /// </summary>
    public class RommScreenshot
    {
        /// <summary>
        /// Gets or sets the unique identifier of the screenshot.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the filename of the screenshot.
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the filename without extension.
        /// </summary>
        [JsonProperty("file_name_no_ext")]
        public string FileNameNoExt { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonProperty("file_size_bytes")]
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets whether the screenshot is featured in the gallery.
        /// </summary>
        [JsonProperty("is_gallery")]
        public bool IsGallery { get; set; }

        /// <summary>
        /// Gets or sets whether the screenshot is publicly visible.
        /// </summary>
        [JsonProperty("is_public")]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents metadata from RomM's internal database for a game.
    /// </summary>
    public class RommGameMeta
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RommGameMeta"/> class with default values.
        /// </summary>
        public RommGameMeta()
        {
            Genres = new List<string>();
            Franchises = new List<string>();
            Companies = new List<string>();
            GameModes = new List<string>();
            AgeRatings = new List<string>();
            PlayerCount = "";
        }

        /// <summary>
        /// Gets or sets the ROM identifier.
        /// </summary>
        [JsonProperty("rom_id")]
        public int? RomId { get; set; }

        /// <summary>
        /// Gets or sets the list of genres.
        /// </summary>
        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        /// <summary>
        /// Gets or sets the list of franchises.
        /// </summary>
        [JsonProperty("franchises")]
        public List<string> Franchises { get; set; }

        /// <summary>
        /// Gets or sets the list of companies.
        /// </summary>
        [JsonProperty("companies")]
        public List<string> Companies { get; set; }

        /// <summary>
        /// Gets or sets the list of game modes.
        /// </summary>
        [JsonProperty("game_modes")]
        public List<string> GameModes { get; set; }

        /// <summary>
        /// Gets or sets the list of age ratings.
        /// </summary>
        [JsonProperty("age_ratings")]
        public List<string> AgeRatings { get; set; }

        /// <summary>
        /// Gets or sets the player count string.
        /// </summary>
        [JsonProperty("player_count")]
        public string PlayerCount { get; set; }

        /// <summary>
        /// Gets or sets the first release date as a Unix timestamp.
        /// </summary>
        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the average user rating.
        /// </summary>
        [JsonProperty("average_rating")]
        public double? AverageRating { get; set; }
    }

    /// <summary>
    /// Represents metadata from Screenscraper for a game.
    /// </summary>
    public class SsMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SsMetadata"/> class with default values.
        /// </summary>
        public SsMetadata()
        {
            Name = "";
            Description = "";
            Developer = "";
            Publisher = "";
            Genre = "";
            ReleaseDate = "";
            Players = "";
            Region = "";
            Language = "";
            SystemText = "";
            Synopsis = "";
            Note = "";
            Media = "";
            Classification = "";
            RomCloneof = "";
            Editeur = "";
            Developpeur = "";
            Joueurs = "";
            Genres = new List<string>();
        }

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the game name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the game description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the game developer.
        /// </summary>
        [JsonProperty("developer")]
        public string Developer { get; set; }

        /// <summary>
        /// Gets or sets the game publisher.
        /// </summary>
        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or sets the game genre.
        /// </summary>
        [JsonProperty("genre")]
        public string Genre { get; set; }

        /// <summary>
        /// Gets or sets the release date string.
        /// </summary>
        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the supported player count.
        /// </summary>
        [JsonProperty("players")]
        public string Players { get; set; }

        /// <summary>
        /// Gets or sets the game rating.
        /// </summary>
        [JsonProperty("rating")]
        public double? Rating { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        [JsonProperty("region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the Screenscraper identifier.
        /// </summary>
        [JsonProperty("ss_id")]
        public int? SsId { get; set; }

        /// <summary>
        /// Gets or sets the system text.
        /// </summary>
        [JsonProperty("system_text")]
        public string SystemText { get; set; }

        /// <summary>
        /// Gets or sets the game synopsis.
        /// </summary>
        [JsonProperty("synopsis")]
        public string Synopsis { get; set; }

        /// <summary>
        /// Gets or sets additional notes.
        /// </summary>
        [JsonProperty("note")]
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the media information.
        /// </summary>
        [JsonProperty("media")]
        public string Media { get; set; }

        /// <summary>
        /// Gets or sets the classification.
        /// </summary>
        [JsonProperty("classification")]
        public string Classification { get; set; }

        /// <summary>
        /// Gets or sets the clone of ROM reference.
        /// </summary>
        [JsonProperty("rom_cloneof")]
        public string RomCloneof { get; set; }

        /// <summary>
        /// Gets or sets the publisher identifier (French: Éditeur).
        /// </summary>
        [JsonProperty("editeur_id")]
        public int? EditeurId { get; set; }

        /// <summary>
        /// Gets or sets the publisher name in French (Éditeur).
        /// </summary>
        [JsonProperty("editeur")]
        public string Editeur { get; set; }

        /// <summary>
        /// Gets or sets the developer identifier (French: Développeur).
        /// </summary>
        [JsonProperty("developpeur_id")]
        public int? DeveloppeurId { get; set; }

        /// <summary>
        /// Gets or sets the developer name in French (Développeur).
        /// </summary>
        [JsonProperty("developpeur")]
        public string Developpeur { get; set; }

        /// <summary>
        /// Gets or sets the player count in French (Joueurs).
        /// </summary>
        [JsonProperty("joueurs")]
        public string Joueurs { get; set; }

        /// <summary>
        /// Gets or sets the list of genres.
        /// </summary>
        [JsonProperty("genres")]
        public List<string> Genres { get; set; }
    }

    /// <summary>
    /// Represents an age rating classification for a game.
    /// </summary>
    public class AgeRating
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgeRating"/> class with default values.
        /// </summary>
        public AgeRating()
        {
            Rating = "";
            Category = "";
            RatingCoverUrl = "";
        }

        /// <summary>
        /// Gets or sets the rating value (e.g., "E", "T", "M").
        /// </summary>
        [JsonProperty("rating")]
        public string Rating { get; set; }

        /// <summary>
        /// Gets or sets the rating category (e.g., "ESRB", "PEGI").
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the URL to the rating cover image.
        /// </summary>
        [JsonProperty("rating_cover_url")]
        public string RatingCoverUrl { get; set; }
    }

    /// <summary>
    /// Represents metadata from IGDB (Internet Game Database) for a game.
    /// </summary>
    public class IgdbMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgdbMetadata"/> class with default values.
        /// </summary>
        public IgdbMetadata()
        {
            YoutubeVideoId = "";
            Genres = new List<string>();
            Franchises = new List<string>();
            AlternativeNames = new List<string>();
            Collections = new List<string>();
            Companies = new List<string>();
            GameModes = new List<string>();
            AgeRatings = new List<AgeRating>();
            Platforms = new List<IgdbPlatform>();
            SimilarGames = new List<SimilarGame>();
        }

        /// <summary>
        /// Gets or sets the total combined rating.
        /// </summary>
        [JsonProperty("total_rating")]
        public double? TotalRating { get; set; }

        /// <summary>
        /// Gets or sets the aggregated rating from critics.
        /// </summary>
        [JsonProperty("aggregated_rating")]
        public double? AggregatedRating { get; set; }

        /// <summary>
        /// Gets or sets the first release date as a Unix timestamp.
        /// </summary>
        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the YouTube video ID for the game trailer.
        /// </summary>
        [JsonProperty("youtube_video_id")]
        public string YoutubeVideoId { get; set; }

        /// <summary>
        /// Gets or sets the list of genres.
        /// </summary>
        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        /// <summary>
        /// Gets or sets the list of franchises.
        /// </summary>
        [JsonProperty("franchises")]
        public List<string> Franchises { get; set; }

        /// <summary>
        /// Gets or sets the list of alternative names.
        /// </summary>
        [JsonProperty("alternative_names")]
        public List<string> AlternativeNames { get; set; }

        /// <summary>
        /// Gets or sets the list of collections the game belongs to.
        /// </summary>
        [JsonProperty("collections")]
        public List<string> Collections { get; set; }

        /// <summary>
        /// Gets or sets the list of companies involved in the game.
        /// </summary>
        [JsonProperty("companies")]
        public List<string> Companies { get; set; }

        /// <summary>
        /// Gets or sets the list of game modes.
        /// </summary>
        [JsonProperty("game_modes")]
        public List<string> GameModes { get; set; }

        /// <summary>
        /// Gets or sets the list of age ratings.
        /// </summary>
        [JsonProperty("age_ratings")]
        public List<AgeRating> AgeRatings { get; set; }

        /// <summary>
        /// Gets or sets the list of platforms the game is available on.
        /// </summary>
        [JsonProperty("platforms")]
        public List<IgdbPlatform> Platforms { get; set; }

        /// <summary>
        /// Gets or sets the list of similar games.
        /// </summary>
        [JsonProperty("similar_games")]
        public List<SimilarGame> SimilarGames { get; set; }
    }

    /// <summary>
    /// Represents a platform from IGDB.
    /// </summary>
    public class IgdbPlatform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgdbPlatform"/> class with default values.
        /// </summary>
        public IgdbPlatform()
        {
            Name = "";
        }

        /// <summary>
        /// Gets or sets the IGDB platform identifier.
        /// </summary>
        [JsonProperty("igdb_id")]
        public int IgdbId { get; set; }

        /// <summary>
        /// Gets or sets the platform name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a game similar to the current game from IGDB.
    /// </summary>
    public class SimilarGame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimilarGame"/> class with default values.
        /// </summary>
        public SimilarGame()
        {
            Name = "";
            Slug = "";
            Type = "";
            CoverUrl = "";
        }

        /// <summary>
        /// Gets or sets the game identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the game name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL-friendly slug.
        /// </summary>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the game type (e.g., "main_game", "dlc").
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the URL to the cover image.
        /// </summary>
        [JsonProperty("cover_url")]
        public string CoverUrl { get; set; }
    }

    /// <summary>
    /// Represents a ROM file associated with a game.
    /// </summary>
    public class RommFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RommFile"/> class with default values.
        /// </summary>
        public RommFile()
        {
            FileName = "";
            FilePath = "";
            FullPath = "";
            Category = "";
            CrcHash = "";
            Md5Hash = "";
            Sha1Hash = "";
        }

        /// <summary>
        /// Gets or sets the unique identifier of the file.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the relative file path.
        /// </summary>
        [JsonProperty("file_path")]
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonProperty("file_size_bytes")]
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the full absolute file path.
        /// </summary>
        [JsonProperty("full_path")]
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the file category (e.g., "rom", "save").
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the CRC hash of the file.
        /// </summary>
        [JsonProperty("crc_hash")]
        public string CrcHash { get; set; }

        /// <summary>
        /// Gets or sets the MD5 hash of the file.
        /// </summary>
        [JsonProperty("md5_hash")]
        public string Md5Hash { get; set; }

        /// <summary>
        /// Gets or sets the SHA1 hash of the file.
        /// </summary>
        [JsonProperty("sha1_hash")]
        public string Sha1Hash { get; set; }
    }
}
