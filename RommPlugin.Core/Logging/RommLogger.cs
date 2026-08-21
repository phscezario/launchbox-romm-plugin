using System;
using System.IO;
using RommPlugin.Core.Storage;

namespace RommPlugin.Core.Logging
{
    public static class RommLogger
    {
        private static readonly string LogDirectory;
        private static bool _enabled;
        private static readonly object _lock = new object();

        static RommLogger()
        {
            LogDirectory = RommPaths.LogsFolder;
            System.Diagnostics.Debug.WriteLine($"[DIAG] RommLogger: LogDirectory={LogDirectory}");
        }

        public static void Initialize(bool enabled, int retentionDays = 7)
        {
            _enabled = enabled;
            CleanupOldLogs(retentionDays);
        }

        private static void CleanupOldLogs(int retentionDays)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                var cutoff = DateTime.Now.AddDays(-retentionDays);
                var logFiles = Directory.GetFiles(LogDirectory, "romm-*.log");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoff)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch
            {
            }
        }

        public static void Log(string message)
        {
            if (!_enabled) return;
            WriteToFile("INFO", message);
        }

        public static void LogError(string message)
        {
            WriteToFile("ERROR", message);
        }

        public static void LogException(Exception ex)
        {
            WriteToFile("ERROR", ex.ToString());
        }

        private static void WriteToFile(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    var filePath = Path.Combine(LogDirectory, $"romm-{DateTime.Now:yyyy-MM-dd}.log");
                    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    File.AppendAllText(filePath, line + Environment.NewLine);
                }
            }
            catch
            {
            }
        }
    }
}
