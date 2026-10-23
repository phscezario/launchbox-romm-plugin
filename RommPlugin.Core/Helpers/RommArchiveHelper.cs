using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Outcome of deleting an installed game path. Tells callers apart
    /// "nothing there" (safe to complete) from "delete failed" (the game
    /// must stay flagged as installed and the user must retry).
    /// </summary>
    public enum GamePathDeleteOutcome
    {
        Deleted,
        AlreadyGone,
        Failed,
    }

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
        /// Best-effort recursive directory deletion: strips read-only flags that
        /// break it and retries transient locks. Logs only the final failure.
        /// Returns true when gone.
        /// </summary>
        public static bool DeleteDirectoryRobust(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            const int attempts = 3;
            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    if (!Directory.Exists(path))
                        return true;
                    Directory.Delete(path, true);
                    return true;
                }
                catch (UnauthorizedAccessException) when (i < attempts - 1)
                {
                    // Often just read-only flags (Synology/extracted media):
                    // strip and retry before giving up.
                    try
                    {
                        StripReadOnly(path, true);
                    }
                    catch
                    {
                    }

                    Thread.Sleep(400);
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(400);
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"[Cleanup] Failed to delete directory '{path}': {ex.Message}");
                    return false;
                }
            }

            return !Directory.Exists(path);
        }

        /// <summary>
        /// Deletes an installed file/folder: strips read-only attributes that
        /// break recursive deletes and retries transient locks, distinguishing
        /// "nothing there" from "delete failed" so uninstalls never complete
        /// while files remain on disk.
        /// </summary>
        public static GamePathDeleteOutcome DeleteGamePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return GamePathDeleteOutcome.AlreadyGone;

            bool isDir = Directory.Exists(path);
            if (!isDir && !File.Exists(path))
                return GamePathDeleteOutcome.AlreadyGone;

            try
            {
                StripReadOnly(path, isDir);
            }
            catch
            {
            }

            const int attempts = 3;
            for (var i = 0; i < attempts; i++)
            {
                bool gone = isDir ? DeleteDirectoryRobust(path) : DeleteFileWithRetry(path, 1, 0);
                if (gone)
                    break;
                Thread.Sleep(400);
            }

            if (!Directory.Exists(path) && !File.Exists(path))
                return GamePathDeleteOutcome.Deleted;

            return GamePathDeleteOutcome.Failed;
        }

        private static void StripReadOnly(string path, bool isDir)
        {
            if (isDir)
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var attributes = File.GetAttributes(file);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                            File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                try
                {
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                }
                catch
                {
                }
            }

            try
            {
                var own = File.GetAttributes(path);
                if ((own & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, own & ~FileAttributes.ReadOnly);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Copies a file tolerating transient locks (antivirus/indexer grab new
        /// files for seconds). Throws the last error after exhausting retries.
        /// </summary>
        public static void CopyFileRobust(string source, string dest)
        {
            const int attempts = 3;
            for (var i = 0; ; i++)
            {
                try
                {
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    StripReadOnly(dest, false);
                    File.Copy(source, dest, true);
                    return;
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(500);
                }
                catch (UnauthorizedAccessException) when (i < attempts - 1)
                {
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Fails fast with a clear message when a directory is not writable,
        /// instead of dying gigabytes into an extraction.
        /// </summary>
        public static void EnsureWritableDirectory(string targetDir)
        {
            try
            {
                Directory.CreateDirectory(targetDir);
                var probe = Path.Combine(targetDir, ".romm_write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "x");
                File.Delete(probe);
            }
            catch (Exception ex)
            {
                throw new IOException($"Destination '{targetDir}' is not writable (locked or access denied): {ex.Message}", ex);
            }
        }
        /// <summary>
        /// Whether a directory name matches this plugin's temp staging pattern:
        /// <c>_temp_</c> + 32 hex chars (flatten extraction) or
        /// <c>&lt;name&gt;._tmp_</c> + 32 hex chars (folder extraction).
        /// Strict on purpose so user folders are never touched.
        /// </summary>
        public static bool IsTempStagingDirName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            const string prefix = "_temp_";
            const string infix = "._tmp_";
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return IsHex32(name.Substring(prefix.Length));
            int i = name.LastIndexOf(infix, StringComparison.Ordinal);
            if (i < 0) return false;
            return IsHex32(name.Substring(i + infix.Length));
        }

        private static bool IsHex32(string value)
        {
            return value.Length == 32 && value.All(c =>
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
