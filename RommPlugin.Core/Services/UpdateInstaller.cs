using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Handles the extraction and installation of pending plugin updates from
    /// a staged zip archive, using a batch script to replace files and restart LaunchBox.
    /// </summary>
    public static class UpdateInstaller
    {
        private static string UpdateDir => Path.Combine(Path.GetTempPath(), "RomMPlugin_Update");
        private static string PendingZipPath => Path.Combine(UpdateDir, "pending.zip");
        private static string PendingFlagPath => Path.Combine(UpdateDir, "update.pending");
        private static string PendingVersionPath => Path.Combine(UpdateDir, "pending.version");
        private static string BatchScriptPath => Path.Combine(UpdateDir, "update.bat");

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
        /// Applies the pending update by extracting the downloaded archive, locating the
        /// plugin directory within it, generating a batch script to replace files, and
        /// launching the script before exiting the current process.
        /// </summary>
        /// <returns><c>true</c> if the update batch was launched successfully; otherwise, <c>false</c>.</returns>
        public static bool ApplyPendingUpdate()
        {
            try
            {
                if (!HasPendingUpdate())
                    return false;

                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var version = GetPendingVersion();

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

                CreateUpdateBatch(sourceDir, pluginDir, version);
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

        private static void CreateUpdateBatch(string sourceDir, string pluginDir, string version)
        {
            var source = sourceDir.Replace("\"", "\\\"");
            var dest = pluginDir.Replace("\"", "\\\"");
            var launchboxPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "LaunchBox.exe").Replace("\"", "\\\"");

            var batch = $@"@echo off
echo RomM Plugin Update - Applying version {version}...
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

        /// <summary>
        /// Removes the temporary update staging directory and all its contents.
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
