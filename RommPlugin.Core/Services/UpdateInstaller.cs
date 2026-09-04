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
        private static string PendingZipPath => Path.Combine(UpdateDir, PendingZipFileName);
        private static string PendingFlagPath => Path.Combine(UpdateDir, PendingFlagFileName);
        private static string PendingVersionPath => Path.Combine(UpdateDir, PendingVersionFileName);

        /// <summary>Staging directory for pending updates (shared with the CLI updater).</summary>
        public static string UpdateDirectory => UpdateDir;

        /// <summary>Staged update archive filename.</summary>
        public const string PendingZipFileName = "pending.zip";

        /// <summary>Flag filename marking a staged update as pending.</summary>
        public const string PendingFlagFileName = "update.pending";

        /// <summary>Filename holding the pending update version.</summary>
        public const string PendingVersionFileName = "pending.version";

        /// <summary>Marker filename written by the CLI when applying fails.</summary>
        public const string FailedMarkerFileName = "update.failed";

        /// <summary>Full path of the failure marker file.</summary>
        public static string FailedMarkerPath => Path.Combine(UpdateDir, FailedMarkerFileName);

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
        /// Applies the pending update by staging the CLI tool outside the plugin
        /// folder (so it holds no locks on the files being replaced), consuming
        /// the pending flag to prevent overlapping runs, then launching the CLI
        /// in <c>--apply-update</c> mode (detached) and exiting the current
        /// process so LaunchBox can be closed, updated and restarted.
        /// If the CLI cannot be launched, the pending flag is restored.
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

                var stagedCliPath = StageCliOutsidePluginFolder(cliPath);
                if (stagedCliPath == null)
                    return false;

                ConsumePendingFlag();

                var psi = new ProcessStartInfo
                {
                    FileName = stagedCliPath,
                    Arguments = $"--apply-update \"{UpdateDir}\" \"{pluginDir}\" \"{launchBoxExe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                try
                {
                    Process.Start(psi);
                }
                catch
                {
                    RestorePendingFlag();
                    throw;
                }

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
        /// Copies the CLI updater and its full dependency closure (config, Core,
        /// Newtonsoft) into the staging directory so the updater process holds
        /// no file locks inside the plugin folder it is about to replace.
        /// </summary>
        /// <returns>Full path of the staged CLI executable, or <c>null</c> on failure.</returns>
        private static string StageCliOutsidePluginFolder(string cliPath)
        {
            try
            {
                var stageBinDir = Path.Combine(UpdateDir, "bin");
                Directory.CreateDirectory(stageBinDir);

                var stagedCliPath = Path.Combine(stageBinDir, RommConstants.CliExecutable);
                File.Copy(cliPath, stagedCliPath, true);

                var pluginDir = Path.GetDirectoryName(cliPath);
                foreach (var file in new[]
                {
                    RommConstants.CliExecutable + ".config",
                    "RommPlugin.Core.dll",
                    "Newtonsoft.Json.dll"
                })
                {
                    var source = Path.Combine(pluginDir, file);
                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(stageBinDir, file), true);
                }

                return stagedCliPath;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to stage update CLI: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the pending flag to claim the update and prevent overlapping
        /// updater runs. The staged CLI recreates it if applying fails.
        /// </summary>
        private static void ConsumePendingFlag()
        {
            try
            {
                if (File.Exists(PendingFlagPath))
                    File.Delete(PendingFlagPath);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to consume pending flag: {ex.Message}");
            }
        }

        /// <summary>
        /// Recreates the pending flag when the updater could not be launched,
        /// so the update is offered again on the next startup.
        /// </summary>
        private static void RestorePendingFlag()
        {
            try
            {
                if (File.Exists(PendingZipPath) && !File.Exists(PendingFlagPath))
                    File.WriteAllText(PendingFlagPath, "pending");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to restore pending flag: {ex.Message}");
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
