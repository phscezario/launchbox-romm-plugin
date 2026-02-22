using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class RommPlatform
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("fs_slug")]
        public string FsSlug { get; set; }

        [JsonProperty("rom_count")]
        public int RomCount { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("igdb_slug")]
        public string IgdbSlug { get; set; }

        [JsonProperty("moby_slug")]
        public string MobySlug { get; set; }

        [JsonProperty("hltb_slug")]
        public string HltbSlug { get; set; }

        [JsonProperty("custom_name")]
        public string CustomName { get; set; }

        [JsonProperty("igdb_id")]
        public int? IgdbId { get; set; }

        [JsonProperty("sgdb_id")]
        public int? SgdbId { get; set; }

        [JsonProperty("moby_id")]
        public int? MobyId { get; set; }

        [JsonProperty("launchbox_id")]
        public int? LaunchboxId { get; set; }

        [JsonProperty("ss_id")]
        public int? SsId { get; set; }

        [JsonProperty("ra_id")]
        public int? RaId { get; set; }

        [JsonProperty("hasheous_id")]
        public int? HasheousId { get; set; }

        [JsonProperty("tgdb_id")]
        public int? TgdbId { get; set; }

        [JsonProperty("flashpoint_id")]
        public int? FlashpointId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("generation")]
        public int? Generation { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("family_slug")]
        public string FamilySlug { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("url_logo")]
        public string UrlLogo { get; set; }

        [JsonProperty("firmware")]
        public List<string> Firmware { get; set; }

        [JsonProperty("aspect_ratio")]
        public string AspectRatio { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("fs_size_bytes")]
        public long FsSizeBytes { get; set; }

        [JsonProperty("is_unidentified")]
        public bool IsUnidentified { get; set; }

        [JsonProperty("is_identified")]
        public bool IsIdentified { get; set; }

        [JsonProperty("missing_from_fs")]
        public bool MissingFromFs { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        public RommPlatform()
        {
            Slug = "";
            FsSlug = "";
            Name = "";
            CustomName = "";
            Category = "";
            Firmware = new List<string>();
            DisplayName = "";
        }
    }
}
