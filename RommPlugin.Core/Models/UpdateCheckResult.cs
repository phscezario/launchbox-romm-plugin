using System;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Represents the result of a plugin update check.
    /// </summary>
    public class UpdateCheckResult
    {
        /// <summary>
        /// Gets or sets whether an update is available.
        /// </summary>
        public bool UpdateAvailable { get; set; }

        /// <summary>
        /// Gets or sets the current installed version.
        /// </summary>
        public Version CurrentVersion { get; set; }

        /// <summary>
        /// Gets or sets the latest available version.
        /// </summary>
        public Version LatestVersion { get; set; }

        /// <summary>
        /// Gets or sets the release notes for the latest version.
        /// </summary>
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// Gets or sets the ZIP asset for downloading the update.
        /// </summary>
        public GitHubReleaseAsset ZipAsset { get; set; }

        /// <summary>
        /// Gets or sets the setup executable asset for the update.
        /// </summary>
        public GitHubReleaseAsset SetupAsset { get; set; }
    }
}
