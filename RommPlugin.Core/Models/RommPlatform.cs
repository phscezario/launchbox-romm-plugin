using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a gaming platform from the RomM server.
    /// </summary>
    public class RommPlatform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RommPlatform"/> class with default values.
        /// </summary>
        public RommPlatform()
        {
            Slug = "";
            FsSlug = "";
            Name = "";
            CustomName = "";
            Category = "";
            Firmware = new List<Firmware>();
            DisplayName = "";
            IgdbSlug = "";
            MobySlug = "";
            HltbSlug = "";
            FamilyName = "";
            FamilySlug = "";
            Url = "";
            UrlLogo = "";
            AspectRatio = "";
        }

        /// <summary>
        /// Gets or sets the unique identifier of the platform.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the URL-friendly slug identifier.
        /// </summary>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the filesystem slug used for directory naming.
        /// </summary>
        [JsonProperty("fs_slug")]
        public string FsSlug { get; set; }

        /// <summary>
        /// Gets or sets the number of ROMs for this platform.
        /// </summary>
        [JsonProperty("rom_count")]
        public int RomCount { get; set; }

        /// <summary>
        /// Gets or sets the platform name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the IGDB slug identifier.
        /// </summary>
        [JsonProperty("igdb_slug")]
        public string IgdbSlug { get; set; }

        /// <summary>
        /// Gets or sets the MobyGames slug identifier.
        /// </summary>
        [JsonProperty("moby_slug")]
        public string MobySlug { get; set; }

        /// <summary>
        /// Gets or sets the HowLongToBeat slug identifier.
        /// </summary>
        [JsonProperty("hltb_slug")]
        public string HltbSlug { get; set; }

        /// <summary>
        /// Gets or sets a custom display name for the platform.
        /// </summary>
        [JsonProperty("custom_name")]
        public string CustomName { get; set; }

        /// <summary>
        /// Gets or sets the IGDB platform identifier.
        /// </summary>
        [JsonProperty("igdb_id")]
        public int? IgdbId { get; set; }

        /// <summary>
        /// Gets or sets the SGDB (SteamGridDB) identifier.
        /// </summary>
        [JsonProperty("sgdb_id")]
        public int? SgdbId { get; set; }

        /// <summary>
        /// Gets or sets the MobyGames identifier.
        /// </summary>
        [JsonProperty("moby_id")]
        public int? MobyId { get; set; }

        /// <summary>
        /// Gets or sets the LaunchBox identifier.
        /// </summary>
        [JsonProperty("launchbox_id")]
        public int? LaunchboxId { get; set; }

        /// <summary>
        /// Gets or sets the Screenscraper identifier.
        /// </summary>
        [JsonProperty("ss_id")]
        public int? SsId { get; set; }

        /// <summary>
        /// Gets or sets the RetroAchievements identifier.
        /// </summary>
        [JsonProperty("ra_id")]
        public int? RaId { get; set; }

        /// <summary>
        /// Gets or sets the Hasheous identifier.
        /// </summary>
        [JsonProperty("hasheous_id")]
        public int? HasheousId { get; set; }

        /// <summary>
        /// Gets or sets the TheGamesDB identifier.
        /// </summary>
        [JsonProperty("tgdb_id")]
        public int? TgdbId { get; set; }

        /// <summary>
        /// Gets or sets the Flashpoint identifier.
        /// </summary>
        [JsonProperty("flashpoint_id")]
        public int? FlashpointId { get; set; }

        /// <summary>
        /// Gets or sets the platform category.
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the console generation number.
        /// </summary>
        [JsonProperty("generation")]
        public int? Generation { get; set; }

        /// <summary>
        /// Gets or sets the platform family name.
        /// </summary>
        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        /// <summary>
        /// Gets or sets the platform family slug.
        /// </summary>
        [JsonProperty("family_slug")]
        public string FamilySlug { get; set; }

        /// <summary>
        /// Gets or sets the URL to the platform information page.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the URL to the platform logo image.
        /// </summary>
        [JsonProperty("url_logo")]
        public string UrlLogo { get; set; }

        /// <summary>
        /// Gets or sets the list of firmware files for the platform.
        /// </summary>
        [JsonProperty("firmware")]
        public List<Firmware> Firmware { get; set; }

        /// <summary>
        /// Gets or sets the display aspect ratio.
        /// </summary>
        [JsonProperty("aspect_ratio")]
        public string AspectRatio { get; set; }

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
        /// Gets or sets the total filesystem size in bytes.
        /// </summary>
        [JsonProperty("fs_size_bytes")]
        public long? FsSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets whether the platform is unidentified.
        /// </summary>
        [JsonProperty("is_unidentified")]
        public bool IsUnidentified { get; set; }

        /// <summary>
        /// Gets or sets whether the platform has been identified.
        /// </summary>
        [JsonProperty("is_identified")]
        public bool IsIdentified { get; set; }

        /// <summary>
        /// Gets or sets whether the platform files are missing from the filesystem.
        /// </summary>
        [JsonProperty("missing_from_fs")]
        public bool MissingFromFs { get; set; }

        /// <summary>
        /// Gets or sets the display name for the platform.
        /// </summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Represents a firmware file required by a platform.
    /// </summary>
    public class Firmware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Firmware"/> class with default values.
        /// </summary>
        public Firmware()
        {
            Name = "";
            FileName = "";
            Sha256 = "";
            Path = "";
        }

        /// <summary>
        /// Gets or sets the unique identifier of the firmware.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the firmware name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the platform identifier this firmware belongs to.
        /// </summary>
        [JsonProperty("platform_id")]
        public int PlatformId { get; set; }

        /// <summary>
        /// Gets or sets the firmware filename.
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the firmware file size.
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the firmware file.
        /// </summary>
        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        /// <summary>
        /// Gets or sets the file path to the firmware.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

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
}
