using System;
using System.IO;

namespace RommPlugin.Core.Helpers
{
    public static class SafeFileWriter
    {
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
