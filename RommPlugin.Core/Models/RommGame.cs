using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class RommGame
    {
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

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("platform_id")]
        public int PlatformId { get; set; }

        [JsonProperty("platform_slug")]
        public string PlatformSlug { get; set; }

        [JsonProperty("platform_fs_slug")]
        public string PlatformFsSlug { get; set; }

        [JsonProperty("platform_display_name")]
        public string PlatformDisplayName { get; set; }

        [JsonProperty("fs_name")]
        public string FsName { get; set; }

        [JsonProperty("fs_name_no_tags")]
        public string FsNameNoTags { get; set; }

        [JsonProperty("fs_name_no_ext")]
        public string FsNameNoExt { get; set; }

        [JsonProperty("fs_extension")]
        public string FsExtension { get; set; }

        [JsonProperty("fs_path")]
        public string FsPath { get; set; }

        [JsonProperty("fs_size_bytes")]
        public long? FsSizeBytes { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("alternative_names")]
        public List<string> AlternativeNames { get; set; }

        [JsonProperty("youtube_video_id")]
        public string YoutubeVideoId { get; set; }

        [JsonProperty("metadatum")]
        public RommGameMeta Metadatum { get; set; }

        [JsonProperty("igdb_metadata")]
        public IgdbMetadata IgdbMetadata { get; set; }

        [JsonProperty("launchbox_metadata")]
        public LaunchBoxMetadataModel LaunchBoxMetadata { get; set; }

        [JsonProperty("ss_metadata")]
        public SsMetadata SsMetadata { get; set; }

        [JsonProperty("path_cover_small")]
        public string PathCoverSmall { get; set; }

        [JsonProperty("path_cover_large")]
        public string PathCoverLarge { get; set; }

        [JsonProperty("url_cover")]
        public string UrlCover { get; set; }

        [JsonProperty("has_manual")]
        public bool HasManual { get; set; }

        [JsonProperty("path_manual")]
        public string PathManual { get; set; }

        [JsonProperty("url_manual")]
        public string UrlManual { get; set; }

        [JsonProperty("is_identified")]
        public bool IsIdentified { get; set; }

        [JsonProperty("is_unidentified")]
        public bool IsUnidentified { get; set; }

        [JsonProperty("missing_from_fs")]
        public bool MissingFromFs { get; set; }

        [JsonProperty("has_simple_single_file")]
        public bool HasSimpleSingleFile { get; set; }

        [JsonProperty("has_nested_single_file")]
        public bool HasNestedSingleFile { get; set; }

        [JsonProperty("has_multiple_files")]
        public bool HasMultipleFiles { get; set; }

        [JsonProperty("files")]
        public List<RommFile> Files { get; set; }

        [JsonProperty("merged_screenshots")]
        public List<string> MergedScreenshots { get; set; }
    }

    public class RommGameMeta
    {
        public RommGameMeta()
        {
            Genres = new List<string>();
            Franchises = new List<string>();
            Companies = new List<string>();
            GameModes = new List<string>();
            AgeRatings = new List<string>();
            PlayerCount = "";
        }

        [JsonProperty("rom_id")]
        public int? RomId { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        [JsonProperty("franchises")]
        public List<string> Franchises { get; set; }

        [JsonProperty("companies")]
        public List<string> Companies { get; set; }

        [JsonProperty("game_modes")]
        public List<string> GameModes { get; set; }

        [JsonProperty("age_ratings")]
        public List<string> AgeRatings { get; set; }

        [JsonProperty("player_count")]
        public string PlayerCount { get; set; }

        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        [JsonProperty("average_rating")]
        public double? AverageRating { get; set; }
    }

    public class SsMetadata
    {
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

        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("developer")]
        public string Developer { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("genre")]
        public string Genre { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        [JsonProperty("players")]
        public string Players { get; set; }

        [JsonProperty("rating")]
        public double? Rating { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("ss_id")]
        public int? SsId { get; set; }

        [JsonProperty("system_text")]
        public string SystemText { get; set; }

        [JsonProperty("synopsis")]
        public string Synopsis { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("media")]
        public string Media { get; set; }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        [JsonProperty("rom_cloneof")]
        public string RomCloneof { get; set; }

        [JsonProperty("editeur_id")]
        public int? EditeurId { get; set; }

        [JsonProperty("editeur")]
        public string Editeur { get; set; }

        [JsonProperty("developpeur_id")]
        public int? DeveloppeurId { get; set; }

        [JsonProperty("developpeur")]
        public string Developpeur { get; set; }

        [JsonProperty("joueurs")]
        public string Joueurs { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }
    }

    public class AgeRating
    {
        public AgeRating()
        {
            Rating = "";
            Category = "";
            RatingCoverUrl = "";
        }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("rating_cover_url")]
        public string RatingCoverUrl { get; set; }
    }

    public class IgdbMetadata
    {
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

        [JsonProperty("total_rating")]
        public double? TotalRating { get; set; }

        [JsonProperty("aggregated_rating")]
        public double? AggregatedRating { get; set; }

        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        [JsonProperty("youtube_video_id")]
        public string YoutubeVideoId { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        [JsonProperty("franchises")]
        public List<string> Franchises { get; set; }

        [JsonProperty("alternative_names")]
        public List<string> AlternativeNames { get; set; }

        [JsonProperty("collections")]
        public List<string> Collections { get; set; }

        [JsonProperty("companies")]
        public List<string> Companies { get; set; }

        [JsonProperty("game_modes")]
        public List<string> GameModes { get; set; }

        [JsonProperty("age_ratings")]
        public List<AgeRating> AgeRatings { get; set; }

        [JsonProperty("platforms")]
        public List<IgdbPlatform> Platforms { get; set; }

        [JsonProperty("similar_games")]
        public List<SimilarGame> SimilarGames { get; set; }
    }

    public class IgdbPlatform
    {
        public IgdbPlatform()
        {
            Name = "";
        }

        [JsonProperty("igdb_id")]
        public int IgdbId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SimilarGame
    {
        public SimilarGame()
        {
            Name = "";
            Slug = "";
            Type = "";
            CoverUrl = "";
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("cover_url")]
        public string CoverUrl { get; set; }
    }

    public class RommFile
    {
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

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        [JsonProperty("file_path")]
        public string FilePath { get; set; }

        [JsonProperty("file_size_bytes")]
        public long? FileSizeBytes { get; set; }

        [JsonProperty("full_path")]
        public string FullPath { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("crc_hash")]
        public string CrcHash { get; set; }

        [JsonProperty("md5_hash")]
        public string Md5Hash { get; set; }

        [JsonProperty("sha1_hash")]
        public string Sha1Hash { get; set; }
    }
}