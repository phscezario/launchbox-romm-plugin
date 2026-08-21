using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class GitHubRelease
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonProperty("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = new List<GitHubReleaseAsset>();

        public Version GetVersion()
        {
            var versionStr = TagName?.TrimStart('v', 'V');
            if (Version.TryParse(versionStr, out var version))
                return version;
            return null;
        }

        public GitHubReleaseAsset GetZipAsset()
        {
            return Assets?.Find(a =>
                a.Name != null &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                !a.Name.Contains("Setup") &&
                !a.Name.Contains("symbols"));
        }

        public GitHubReleaseAsset GetSetupAsset()
        {
            return Assets?.Find(a =>
                a.Name != null &&
                (a.Name.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase) ||
                 a.Name.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase)));
        }
    }

    public class GitHubReleaseAsset
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("download_count")]
        public long DownloadCount { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
