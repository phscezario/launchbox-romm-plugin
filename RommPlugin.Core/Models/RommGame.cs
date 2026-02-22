using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class RommGame
    {
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
        public long FsSizeBytes { get; set; }

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
        public object LaunchBoxMetadata { get; set; }

        [JsonProperty("ss_metadata")]
        public object SsMetadata { get; set; }

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

    public class AgeRating
    {
        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("rating_cover_url")]
        public string RatingCoverUrl { get; set; }
    }

    public class IgdbMetadata
    {
        [JsonProperty("total_rating")]
        public double? TotalRating { get; set; }

        [JsonProperty("aggregated_rating")]
        public double ?AggregatedRating { get; set; }

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
        [JsonProperty("igdb_id")]
        public int IgdbId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SimilarGame
    {
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
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        [JsonProperty("file_path")]
        public string FilePath { get; set; }

        [JsonProperty("file_size_bytes")]
        public long FileSizeBytes { get; set; }

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
