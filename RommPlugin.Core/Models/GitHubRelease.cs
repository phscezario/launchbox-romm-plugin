using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a GitHub release for the plugin.
    /// </summary>
    public class GitHubRelease
    {
        /// <summary>
        /// Gets or sets the unique identifier of the release.
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the release tag name (e.g., "v1.0.0").
        /// </summary>
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the release notes body.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets whether this is a draft release.
        /// </summary>
        [JsonProperty("draft")]
        public bool Draft { get; set; }

        /// <summary>
        /// Gets or sets whether this is a pre-release.
        /// </summary>
        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the publication timestamp.
        /// </summary>
        [JsonProperty("published_at")]
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// Gets or sets the list of release assets.
        /// </summary>
        [JsonProperty("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = new List<GitHubReleaseAsset>();

        /// <summary>
        /// Parses and returns the version from the tag name.
        /// </summary>
        /// <returns>The parsed version, or null if parsing fails.</returns>
        public Version GetVersion()
        {
            var versionStr = TagName?.TrimStart('v', 'V');
            if (Version.TryParse(versionStr, out var version))
                return version;
            return null;
        }

        /// <summary>
        /// Gets the ZIP archive asset from the release, excluding setup and symbols files.
        /// </summary>
        /// <returns>The ZIP asset, or null if not found.</returns>
        public GitHubReleaseAsset GetZipAsset()
        {
            return Assets?.Find(a =>
                a.Name != null &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                !a.Name.Contains("Setup") &&
                !a.Name.Contains("symbols"));
        }

        /// <summary>
        /// Gets the setup executable asset from the release.
        /// </summary>
        /// <returns>The setup asset, or null if not found.</returns>
        public GitHubReleaseAsset GetSetupAsset()
        {
            return Assets?.Find(a =>
                a.Name != null &&
                (a.Name.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase) ||
                 a.Name.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase)));
        }
    }

    /// <summary>
    /// Represents a downloadable asset attached to a GitHub release.
    /// </summary>
    public class GitHubReleaseAsset
    {
        /// <summary>
        /// Gets or sets the unique identifier of the asset.
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the asset filename.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the asset label.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the content type (MIME type) of the asset.
        /// </summary>
        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the asset file size in bytes.
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the number of times the asset has been downloaded.
        /// </summary>
        [JsonProperty("download_count")]
        public long DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets the browser download URL for the asset.
        /// </summary>
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
