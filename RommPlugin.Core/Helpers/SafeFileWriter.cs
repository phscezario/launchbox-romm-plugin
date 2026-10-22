using System;
using System.IO;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Provides atomic file write operations to prevent data corruption during crashes.
    /// Writes to a temporary file first, then swaps it into the target location.
    /// </summary>
    public static class SafeFileWriter
    {
        /// <summary>
        /// Writes content to a file atomically using a temporary file intermediate.
        /// If the process crashes mid-write, the original file remains intact.
        /// </summary>
        /// <remarks>
        /// The swap uses <see cref="File.Replace(string,string,string)"/> so a
        /// concurrent reader sees either the previous or the new file, never a
        /// partially written one. Two LaunchBox instances share these files
        /// (state, settings, installed games) and a reader that caught a
        /// truncated JSON treated it as "nothing to do" — which silently dropped
        /// pending installs. Older behavior (copy over the target) is kept as a
        /// fallback for volumes where Replace is unavailable.
        /// </remarks>
        /// <param name="targetPath">The full path to the target file.</param>
        /// <param name="content">The content to write to the file.</param>
        public static void WriteAllText(string targetPath, string content)
        {
            var dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var tempPath = Path.Combine(dir ?? ".", $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(tempPath, content);

                if (!File.Exists(targetPath))
                {
                    try
                    {
                        // A move inside the same directory is atomic: a concurrent
                        // reader never sees the brand-new file half written.
                        File.Move(tempPath, targetPath);
                        return;
                    }
                    catch (IOException)
                    {
                        // Another writer created the file between the check and
                        // the move. Fall through and replace its content: two
                        // LaunchBox instances share these files, and abandoning
                        // the write here silently dropped it.
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                try
                {
                    File.Replace(tempPath, targetPath, null);
                    return;
                }
                catch (IOException)
                {
                    // Filesystem without Replace support: fall back to the
                    // previous non-atomic overwrite. (A locked reader is not
                    // fixed by this path; it fails and is handled by callers.)
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (PlatformNotSupportedException)
                {
                }

                File.Copy(tempPath, targetPath, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }
}
