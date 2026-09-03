using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Checks for plugin updates from GitHub releases and manages the download
    /// and staging of update artifacts.
    /// </summary>
    public static class GitHubUpdateService
    {
        private const string GitHubOwner = "phscezario";
        private const string GitHubRepo = "launchbox-romm-plugin";
        private static readonly string ReleasesApiUrl = "https://api.github.com/repos/" + GitHubOwner + "/" + GitHubRepo + "/releases/latest";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        private static string UpdateDir => Path.Combine(Path.GetTempPath(), "RomMPlugin_Update");
        private static string PendingZipPath => Path.Combine(UpdateDir, "pending.zip");
        private static string PendingFlagPath => Path.Combine(UpdateDir, "update.pending");
        private static string PendingVersionPath => Path.Combine(UpdateDir, "pending.version");

        static GitHubUpdateService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("RomM-LaunchBox-Plugin");
        }

        /// <summary>
        /// Gets the version of the currently executing plugin assembly.
        /// </summary>
        /// <returns>The current assembly version.</returns>
        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// Determines whether a previously downloaded update is pending installation.
        /// </summary>
        /// <returns><c>true</c> if a pending update exists; otherwise, <c>false</c>.</returns>
        public static bool HasPendingUpdate() => UpdateInstaller.HasPendingUpdate();

        /// <summary>
        /// Gets the version string of the pending update, if one exists.
        /// </summary>
        /// <returns>The pending version string, or <c>null</c> if no update is pending.</returns>
        public static string GetPendingVersion() => UpdateInstaller.GetPendingVersion();

        /// <summary>
        /// Checks the GitHub releases API for a newer version of the plugin.
        /// </summary>
        /// <returns>An <see cref="UpdateCheckResult"/> containing version info, release notes, and download assets.</returns>
        public static async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            var currentVersion = GetCurrentVersion();
            var result = new UpdateCheckResult
            {
                CurrentVersion = currentVersion
            };

            try
            {
                RommLogger.Log($"Checking for updates... Current version: {currentVersion}");

                using (var response = await _http.GetAsync(ReleasesApiUrl))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var release = JsonConvert.DeserializeObject<GitHubRelease>(json);

                    if (release == null || release.Draft)
                    {
                        RommLogger.Log("No release found or release is draft");
                        return result;
                    }

                    var latestVersion = release.GetVersion();
                    if (latestVersion == null)
                    {
                        RommLogger.Log($"Could not parse version from tag: {release.TagName}");
                        return result;
                    }

                    result.LatestVersion = latestVersion;
                    result.ReleaseNotes = release.Body;
                    result.ZipAsset = release.GetZipAsset();
                    result.SetupAsset = release.GetSetupAsset();
                    result.UpdateAvailable = latestVersion > currentVersion;

                    if (result.UpdateAvailable)
                    {
                        RommLogger.Log($"Update available: {currentVersion} -> {latestVersion}");
                    }
                    else
                    {
                        RommLogger.Log($"Already on latest version: {currentVersion}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to check for updates: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Downloads a release asset to the local update staging directory and marks it
        /// as pending for installation on the next launch.
        /// </summary>
        /// <param name="asset">The GitHub release asset to download.</param>
        /// <param name="version">The version string of the update being downloaded.</param>
        /// <param name="progressCallback">Optional callback invoked with download progress (0-100).</param>
        /// <returns><c>true</c> if the download succeeded; otherwise, <c>false</c>.</returns>
        public static async Task<bool> DownloadUpdateAsync(GitHubReleaseAsset asset, string version, Action<int> progressCallback = null)
        {
            try
            {
                Directory.CreateDirectory(UpdateDir);

                RommLogger.Log($"Downloading update {version} from {asset.BrowserDownloadUrl}");
                progressCallback?.Invoke(0);

                using (var response = await _http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(PendingZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[RommConstants.HttpBufferSize];
                        long bytesRead = 0;
                        int read;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            bytesRead += read;

                            if (totalBytes > 0)
                            {
                                var progress = (int)((bytesRead * 100) / totalBytes);
                                progressCallback?.Invoke(progress);
                            }
                        }
                    }
                }

                File.WriteAllText(PendingVersionPath, version);
                File.WriteAllText(PendingFlagPath, "pending");

                RommLogger.Log($"Update downloaded and saved as pending: {version}");
                progressCallback?.Invoke(100);
                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to download update: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies the pending update by extracting the downloaded archive and launching
        /// a batch script that replaces the plugin files and restarts LaunchBox.
        /// </summary>
        /// <returns><c>true</c> if the update was applied successfully; otherwise, <c>false</c>.</returns>
        public static bool ApplyPendingUpdate() => UpdateInstaller.ApplyPendingUpdate();

        /// <summary>
        /// Removes the temporary update staging directory and all its contents.
        /// </summary>
        public static void CleanupUpdateDir() => UpdateInstaller.CleanupUpdateDir();
    }
}
