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

        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public static bool HasPendingUpdate() => UpdateInstaller.HasPendingUpdate();

        public static string GetPendingVersion() => UpdateInstaller.GetPendingVersion();

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

        public static bool ApplyPendingUpdate() => UpdateInstaller.ApplyPendingUpdate();

        public static void CleanupUpdateDir() => UpdateInstaller.CleanupUpdateDir();
    }
}
