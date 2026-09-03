using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Hands a staged (downloaded) plugin update to the CLI tool, which kills
    /// LaunchBox, copies the new files, restarts LaunchBox and removes all
    /// staging files (including the downloaded zip).
    /// </summary>
    public static class UpdateInstaller
    {
        private static string UpdateDir => Path.Combine(Path.GetTempPath(), "RomMPlugin_Update");
        private static string PendingZipPath => Path.Combine(UpdateDir, "pending.zip");
        private static string PendingFlagPath => Path.Combine(UpdateDir, "update.pending");
        private static string PendingVersionPath => Path.Combine(UpdateDir, "pending.version");

        /// <summary>
        /// Determines whether a previously downloaded update is pending installation
        /// by checking for the presence of both the flag file and the zip archive.
        /// </summary>
        /// <returns><c>true</c> if a pending update exists; otherwise, <c>false</c>.</returns>
        public static bool HasPendingUpdate()
        {
            return File.Exists(PendingFlagPath) && File.Exists(PendingZipPath);
        }

        /// <summary>
        /// Gets the version string of the pending update, read from the pending version file.
        /// </summary>
        /// <returns>The pending version string, or <c>null</c> if no version file exists.</returns>
        public static string GetPendingVersion()
        {
            if (File.Exists(PendingVersionPath))
                return File.ReadAllText(PendingVersionPath).Trim();
            return null;
        }

        /// <summary>
        /// Applies the pending update by launching the CLI tool in
        /// <c>--apply-update</c> mode (detached) and exiting the current
        /// process so LaunchBox can be closed, updated and restarted.
        /// </summary>
        /// <returns><c>true</c> if the CLI updater was launched successfully; otherwise, <c>false</c>.</returns>
        public static bool ApplyPendingUpdate()
        {
            try
            {
                if (!HasPendingUpdate())
                    return false;

                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var version = GetPendingVersion();

                RommLogger.Log($"Applying pending update {version}...");

                var cliPath = Path.Combine(pluginDir, RommConstants.CliExecutable);
                if (!File.Exists(cliPath))
                {
                    RommLogger.LogError($"Update CLI not found at {cliPath}");
                    return false;
                }

                var launchBoxExe = Path.GetFullPath(Path.Combine(pluginDir, "..", "..", "LaunchBox.exe"));
                if (!File.Exists(launchBoxExe))
                {
                    RommLogger.LogError($"LaunchBox.exe not found at {launchBoxExe}");
                    return false;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"--apply-update \"{UpdateDir}\" \"{pluginDir}\" \"{launchBoxExe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
                RommLogger.Log("Update CLI launched, exiting LaunchBox process...");

                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to launch update CLI: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes the temporary update staging directory and all its contents
        /// (zip archive, flag/version files, extracted files).
        /// </summary>
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
