using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Result of an orphan cleanup pass: every path actually removed.
    /// </summary>
    public sealed class OrphanCleanupResult
    {
        /// <summary>Paths removed by the cleanup pass.</summary>
        public List<string> Removed { get; } = new List<string>();

        /// <summary>Number of removed paths.</summary>
        public int TotalRemoved => Removed.Count;
    }

    /// <summary>
    /// Removes leftover artifacts from failed or partial install/uninstall attempts.
    /// Two modes: scoped (a single game, used on every install/uninstall) and full
    /// (plugin-owned temp patterns only, used by the Game Manager Clear button).
    /// Never deletes another game's files and never escapes <c>romsRoot</c>.
    /// </summary>
    public static class RommOrphanCleanupService
    {
        /// <summary>Temp dirs / part files older than this are considered stale.</summary>
        public static readonly TimeSpan DefaultStaleAge = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Cleans artifacts attributable to a single install/uninstall attempt.
        /// </summary>
        /// <param name="romsRoot">ROMs root; nothing outside it is touched.</param>
        /// <param name="gameId">RomM game id, for logging only.</param>
        /// <param name="title">Game title, for logging only.</param>
        /// <param name="copiedFiles">Files copied by the failed attempt (flatten); removed.</param>
        /// <param name="extractDir">Sibling extract dir; removed only when <paramref name="deleteExtractDir"/> is true.</param>
        /// <param name="deleteExtractDir">True when the dir was created by this attempt (did not exist before).</param>
        /// <param name="partFilePath">Download .part file of this game; removed when present.</param>
        /// <param name="zipPath">Source zip; removed only when <paramref name="deleteZip"/> is true (corrupt archive).</param>
        /// <param name="deleteZip">True only for unreadable/corrupt archives.</param>
        /// <param name="staleTempAge">Age after which sibling _temp_* dirs are stale.</param>
        public static OrphanCleanupResult CleanInstallAttempt(
            string romsRoot,
            int gameId,
            string title,
            IEnumerable<string> copiedFiles,
            string extractDir,
            bool deleteExtractDir,
            string partFilePath,
            string zipPath,
            bool deleteZip,
            TimeSpan? staleTempAge = null)
        {
            var result = new OrphanCleanupResult();
            var staleAge = staleTempAge ?? DefaultStaleAge;

            if (copiedFiles != null)
            {
                foreach (var file in copiedFiles)
                {
                    if (TryDeleteFile(romsRoot, file))
                        result.Removed.Add(file);
                }
            }

            if (deleteExtractDir && !string.IsNullOrWhiteSpace(extractDir))
            {
                if (RommArchiveHelper.IsUnderRoot(romsRoot, extractDir)
                    && RommArchiveHelper.DeleteDirectoryRobust(extractDir))
                {
                    result.Removed.Add(extractDir);
                }
            }

            if (!string.IsNullOrWhiteSpace(partFilePath)
                && RommArchiveHelper.IsUnderRoot(romsRoot, partFilePath)
                && RommArchiveHelper.DeleteFileWithRetry(partFilePath, 1, 0))
            {
                result.Removed.Add(partFilePath);
            }

            if (deleteZip && !string.IsNullOrWhiteSpace(zipPath))
            {
                if (RommArchiveHelper.IsUnderRoot(romsRoot, zipPath)
                    && RommArchiveHelper.DeleteFileWithRetry(zipPath))
                {
                    result.Removed.Add(zipPath);
                }
            }

            // Stale staging dirs next to this game's files (from older crashed runs).
            foreach (var parent in ParentDirsOf(zipPath, extractDir))
            {
                foreach (var stale in FindStaleTempDirs(parent, staleAge))
                {
                    if (RommArchiveHelper.DeleteDirectoryRobust(stale))
                        result.Removed.Add(stale);
                }
            }

            if (result.TotalRemoved > 0)
                RommLogger.Log($"[Cleanup] Game {gameId} '{title}': removed {result.TotalRemoved} leftover(s): {string.Join(", ", result.Removed)}");

            return result;
        }

        /// <summary>
        /// Cleans leftovers attributable to a single uninstalled game:
        /// stale staging dirs in the former install folder's parent.
        /// The game files themselves are handled by the uninstall flow.
        /// </summary>
        public static OrphanCleanupResult CleanUninstallLeftovers(
            string romsRoot,
            int gameId,
            string title,
            string installedPath,
            TimeSpan? staleTempAge = null)
        {
            var result = new OrphanCleanupResult();
            var staleAge = staleTempAge ?? DefaultStaleAge;

            string parent = null;
            try
            {
                parent = !string.IsNullOrWhiteSpace(installedPath)
                    ? Path.GetDirectoryName(Path.GetFullPath(installedPath))
                    : null;
            }
            catch
            {
                parent = null;
            }

            if (!string.IsNullOrWhiteSpace(parent))
            {
                foreach (var stale in FindStaleTempDirs(parent, staleAge))
                {
                    if (RommArchiveHelper.IsUnderRoot(romsRoot, stale)
                        && RommArchiveHelper.DeleteDirectoryRobust(stale))
                    {
                        result.Removed.Add(stale);
                    }
                }
            }

            if (result.TotalRemoved > 0)
                RommLogger.Log($"[Cleanup] Game {gameId} '{title}': removed {result.TotalRemoved} leftover(s): {string.Join(", ", result.Removed)}");

            return result;
        }

        /// <summary>
        /// Full sweep of plugin-owned temp patterns. Conservative on purpose:
        /// stale staging dirs, .part files with no matching queue entry, and
        /// abandoned download-state temp files. Never deletes game folders or ROMs.
        /// </summary>
        /// <param name="romsRoot">ROMs root for staging/.part sweep.</param>
        /// <param name="pluginFolder">Plugin folder for download-state temp sweep.</param>
        /// <param name="knownPartFiles">.part paths currently owned by the queue; kept.</param>
        public static OrphanCleanupResult CleanFull(
            string romsRoot,
            string pluginFolder,
            IEnumerable<string> knownPartFiles,
            TimeSpan? staleTempAge = null)
        {
            var result = new OrphanCleanupResult();
            var staleAge = staleTempAge ?? DefaultStaleAge;
            var known = new HashSet<string>(
                (knownPartFiles ?? Enumerable.Empty<string>())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(SafeFullPath)
                    .Where(p => p != null),
                StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(romsRoot) && Directory.Exists(romsRoot))
            {
                IEnumerable<string> dirs = Enumerable.Empty<string>();
                IEnumerable<string> parts = Enumerable.Empty<string>();
                try
                {
                    dirs = Directory.EnumerateDirectories(romsRoot, "_temp_*", SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"[Cleanup] Full sweep directory enumeration failed: {ex.Message}");
                }

                foreach (var dir in dirs)
                {
                    try
                    {
                        if (!RommArchiveHelper.IsTempStagingDirName(Path.GetFileName(dir)))
                            continue;
                        if (!IsStale(dir, staleAge))
                            continue;
                        if (RommArchiveHelper.DeleteDirectoryRobust(dir))
                            result.Removed.Add(dir);
                    }
                    catch
                    {
                    }
                }

                try
                {
                    parts = Directory.EnumerateFiles(romsRoot, "*.part", SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"[Cleanup] Full sweep file enumeration failed: {ex.Message}");
                }

                foreach (var part in parts)
                {
                    try
                    {
                        var full = SafeFullPath(part);
                        if (full == null || known.Contains(full))
                            continue;
                        if (!IsStale(part, staleAge))
                            continue;
                        if (RommArchiveHelper.DeleteFileWithRetry(part, 1, 0))
                            result.Removed.Add(part);
                    }
                    catch
                    {
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(pluginFolder) && Directory.Exists(pluginFolder))
            {
                string[] tmps = new string[0];
                try
                {
                    tmps = Directory.GetFiles(pluginFolder, "download-state.*.tmp");
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"[Cleanup] Full sweep plugin temp enumeration failed: {ex.Message}");
                }

                foreach (var tmp in tmps)
                {
                    try
                    {
                        if (!IsStale(tmp, staleAge))
                            continue;
                        if (RommArchiveHelper.DeleteFileWithRetry(tmp, 1, 0))
                            result.Removed.Add(tmp);
                    }
                    catch
                    {
                    }
                }
            }

            RommLogger.Log($"[Cleanup] Full sweep removed {result.TotalRemoved} orphan(s).");
            return result;
        }

        private static bool TryDeleteFile(string romsRoot, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            if (!RommArchiveHelper.IsUnderRoot(romsRoot, path))
                return false;
            return RommArchiveHelper.DeleteFileWithRetry(path, 1, 0);
        }

        private static IEnumerable<string> ParentDirsOf(params string[] paths)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                string parent = null;
                try
                {
                    parent = Path.GetDirectoryName(Path.GetFullPath(path));
                }
                catch
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(parent)
                    && Directory.Exists(parent)
                    && seen.Add(parent))
                {
                    yield return parent;
                }
            }
        }

        private static IEnumerable<string> FindStaleTempDirs(string parentDir, TimeSpan staleAge)
        {
            string[] dirs = new string[0];
            try
            {
                dirs = Directory.GetDirectories(parentDir, "_temp_*");
            }
            catch
            {
                yield break;
            }

            foreach (var dir in dirs)
            {
                string name = null;
                try
                {
                    name = Path.GetFileName(dir);
                }
                catch
                {
                    continue;
                }
                if (!RommArchiveHelper.IsTempStagingDirName(name))
                    continue;
                if (!IsStale(dir, staleAge))
                    continue;
                yield return dir;
            }
        }

        private static bool IsStale(string path, TimeSpan staleAge)
        {
            try
            {
                DateTime stamp;
                if (Directory.Exists(path))
                    stamp = Directory.GetLastWriteTimeUtc(path);
                else if (File.Exists(path))
                    stamp = File.GetLastWriteTimeUtc(path);
                else
                    return false;
                return (DateTime.UtcNow - stamp) > staleAge;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
