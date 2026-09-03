using System;
using System.IO;
using RommPlugin.Core.Storage;

namespace RommPlugin.Core.Logging
{
    /// <summary>
    /// Provides file-based logging with automatic date-based log rotation and cleanup.
    /// Logs are written to daily files in the format <c>romm-YYYY-MM-DD.log</c>.
    /// </summary>
    public static class RommLogger
    {
        private static readonly string LogDirectory;
        private static bool _enabled;
        private static readonly object _lock = new object();

        static RommLogger()
        {
            LogDirectory = RommPaths.LogsFolder;
        }

        /// <summary>
        /// Initializes the logger and cleans up log files older than the specified retention period.
        /// </summary>
        /// <param name="enabled">When true, info-level logging is enabled. Errors are always logged.</param>
        /// <param name="retentionDays">Number of days to retain log files before deletion.</param>
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

        /// <summary>
        /// Writes an informational message to the log file. Only written when logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            if (!_enabled) return;
            WriteToFile("INFO", message);
        }

        /// <summary>
        /// Writes an error message to the log file. Always written regardless of logging enabled state.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(string message)
        {
            WriteToFile("ERROR", message);
        }

        /// <summary>
        /// Writes an exception details to the log file. Always written regardless of logging enabled state.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
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
