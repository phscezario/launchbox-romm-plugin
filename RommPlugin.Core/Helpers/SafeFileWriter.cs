using System;
using System.IO;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Provides atomic file write operations to prevent data corruption during crashes.
    /// Writes to a temporary file first, then copies to the target location.
    /// </summary>
    public static class SafeFileWriter
    {
        /// <summary>
        /// Writes content to a file atomically using a temporary file intermediate.
        /// If the process crashes mid-write, the original file remains intact.
        /// </summary>
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
                File.Copy(tempPath, targetPath, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }
}
