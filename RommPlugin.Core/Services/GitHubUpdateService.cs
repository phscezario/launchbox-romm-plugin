using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Logging;
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

    public static class GitHubUpdateService
    {
        private const string GitHubOwner = "anomalyco";
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
        private static string BatchScriptPath => Path.Combine(UpdateDir, "update.bat");

        static GitHubUpdateService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("RomM-LaunchBox-Plugin");
        }

        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public static bool HasPendingUpdate()
        {
            return File.Exists(PendingFlagPath) && File.Exists(PendingZipPath);
        }

        public static string GetPendingVersion()
        {
            if (File.Exists(PendingVersionPath))
                return File.ReadAllText(PendingVersionPath).Trim();
            return null;
        }

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
                        var buffer = new byte[8192];
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

        public static bool ApplyPendingUpdate()
        {
            try
            {
                RommLogger.Log("[DIAG] GitHubUpdateService.ApplyPendingUpdate: called");
                if (!HasPendingUpdate())
                {
                    RommLogger.Log("[DIAG] GitHubUpdateService.ApplyPendingUpdate: no pending update");
                    return false;
                }

                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var version = GetPendingVersion();
                RommLogger.Log($"[DIAG] GitHubUpdateService.ApplyPendingUpdate: pluginDir={pluginDir}, version={version}");

                var launchboxPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "LaunchBox.exe");
                RommLogger.Log($"[DIAG] GitHubUpdateService.ApplyPendingUpdate: launchboxPath={Path.GetFullPath(launchboxPath)}, exists={File.Exists(launchboxPath)}");

                RommLogger.Log($"Applying pending update {version}...");

                var extractDir = Path.Combine(UpdateDir, "extracted");
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);

                ZipFile.ExtractToDirectory(PendingZipPath, extractDir);

                var sourceDir = FindPluginDirectory(extractDir);
                if (sourceDir == null)
                {
                    RommLogger.LogError("Could not find plugin directory in downloaded archive");
                    CleanupUpdateDir();
                    return false;
                }

                CreateUpdateBatch(sourceDir, pluginDir);
                return LaunchBatchAndExit();
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to apply pending update: {ex.Message}");
                CleanupUpdateDir();
                return false;
            }
        }

        private static string FindPluginDirectory(string rootDir)
        {
            var pluginName = "RomM LaunchBox Integration";

            var directPath = Path.Combine(rootDir, pluginName);
            if (Directory.Exists(directPath))
                return directPath;

            var found = Directory.GetDirectories(rootDir, pluginName, SearchOption.AllDirectories);
            if (found.Length > 0)
                return found[0];

            var dllPath = Directory.GetFiles(rootDir, "RommPlugin.dll", SearchOption.AllDirectories);
            if (dllPath.Length > 0)
                return Path.GetDirectoryName(dllPath[0]);

            return null;
        }

        private static void CreateUpdateBatch(string sourceDir, string pluginDir)
        {
            var source = sourceDir.Replace("\"", "\\\"");
            var dest = pluginDir.Replace("\"", "\\\"");
            var launchboxPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "LaunchBox.exe").Replace("\"", "\\\"");

            var batch = $@"@echo off
echo RomM Plugin Update - Applying version {GetPendingVersion()}...
echo.
echo Waiting for LaunchBox to close...
timeout /t 3 /nobreak > nul
echo.
echo Copying files...
xcopy /Y /E ""{source}\*"" ""{dest}\"" > nul 2>&1
echo.
echo Cleaning up old files...
del /Q ""{dest}\*.new"" 2>nul
echo.
echo Starting LaunchBox...
start """" ""{launchboxPath}""
echo.
echo Cleaning up update files...
timeout /t 2 /nobreak > nul
rmdir /S /Q ""{UpdateDir}"" 2>nul
del ""%~f0"" 2>nul
";

            File.WriteAllText(BatchScriptPath, batch);
            RommLogger.Log($"Update batch created: {BatchScriptPath}");
        }

        private static bool LaunchBatchAndExit()
        {
            try
            {
                RommLogger.Log("Launching update batch and exiting...");

                Process.Start(new ProcessStartInfo
                {
                    FileName = BatchScriptPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to launch update batch: {ex.Message}");
                return false;
            }
        }

        public static void CleanupUpdateDir()
        {
            try
            {
                if (Directory.Exists(UpdateDir))
                    Directory.Delete(UpdateDir, true);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to cleanup update directory: {ex.Message}");
            }
        }
    }
}
