using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Small filesystem helpers for archive handling: zip readability checks and
    /// best-effort deletions (tolerant to antivirus/Explorer file locks).
    /// </summary>
    public static class RommArchiveHelper
    {
        /// <summary>
        /// Checks whether a zip file can be opened and its central directory read.
        /// Returns false for missing files, non-zip content and truncated archives.
        /// </summary>
        public static bool IsZipReadable(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    var _ = archive.Entries.Count;
                }
                return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a file, retrying a few times to survive transient locks
        /// (antivirus, Explorer). Returns true when the file is gone.
        /// </summary>
        public static bool DeleteFileWithRetry(string path, int attempts = 3, int delayMs = 250)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    if (!File.Exists(path))
                        return true;
                    File.Delete(path);
                    return true;
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException) when (i < attempts - 1)
                {
                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"[Cleanup] Failed to delete file '{path}': {ex.Message}");
                    return false;
                }
            }

            try
            {
                if (!File.Exists(path))
                    return true;
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[Cleanup] Failed to delete file '{path}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Best-effort recursive directory deletion. Returns true when gone.
        /// </summary>
        public static bool DeleteDirectoryRobust(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            try
            {
                if (!Directory.Exists(path))
                    return true;
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[Cleanup] Failed to delete directory '{path}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Whether a directory name matches this plugin's temp staging pattern
        /// ("_temp_" + 32 hex chars, as created by the flatten extraction).
        /// Strict on purpose so user folders are never touched.
        /// </summary>
        public static bool IsTempStagingDirName(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith("_temp_", StringComparison.Ordinal))
                return false;
            var suffix = name.Substring("_temp_".Length);
            return suffix.Length == 32 && suffix.All(c =>
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F'));
        }

        /// <summary>
        /// Whether <paramref name="path"/> is contained in <paramref name="root"/>.
        /// Safety guard so cleanup never escapes the ROMs folder.
        /// </summary>
        public static bool IsUnderRoot(string root, string path)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var pathFull = Path.GetFullPath(path);
                return pathFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pathFull, rootFull, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
