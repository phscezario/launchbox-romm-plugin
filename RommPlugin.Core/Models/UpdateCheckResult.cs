using System;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public Version CurrentVersion { get; set; }
        public Version LatestVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public GitHubReleaseAsset ZipAsset { get; set; }
        public GitHubReleaseAsset SetupAsset { get; set; }
    }
}
