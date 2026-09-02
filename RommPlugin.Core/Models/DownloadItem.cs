using System;
using System.Threading;
using Newtonsoft.Json;
using RommPlugin.Core.Locale;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a file download item with progress tracking and status information.
    /// </summary>
    public class DownloadItem
    {
        /// <summary>
        /// Gets or sets the RomM game identifier.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the game.
        /// </summary>
        public string GameName { get; set; }

        /// <summary>
        /// Gets or sets the filesystem name of the ROM.
        /// </summary>
        public string FsName { get; set; }

        /// <summary>
        /// Gets or sets the filesystem path on the server.
        /// </summary>
        public string FsPath { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes received so far.
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// Gets or sets the total file size in bytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the current download status.
        /// </summary>
        public DownloadStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the local file path where the download is saved.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the path to the temporary partial download file.
        /// </summary>
        public string PartFilePath { get; set; }

        /// <summary>
        /// Gets or sets the current download speed in bytes per second.
        /// </summary>
        public double SpeedBytesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the estimated time remaining for the download.
        /// </summary>
        public TimeSpan EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Gets or sets the number of retry attempts made.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the download was added to the queue.
        /// </summary>
        public DateTime AddedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the download completed, or null if not yet complete.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message if the download failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Internal tracking field for bytes received at the last speed calculation.
        /// </summary>
        internal long _lastBytesReceived;

        /// <summary>
        /// Internal tracking field for the last speed calculation timestamp.
        /// </summary>
        internal DateTime _lastUpdateTime;

        /// <summary>
        /// Gets or sets the per-item cancellation token source used to stop this download.
        /// </summary>
        [JsonIgnore]
        public CancellationTokenSource Cts { get; set; }

        /// <summary>
        /// Gets the download progress as a percentage (0-100).
        /// </summary>
        public int Percentage
        {
            get
            {
                if (TotalBytes <= 0) return 0;
                return (int)Math.Min(100, (BytesReceived * 100) / TotalBytes);
            }
        }

        /// <summary>
        /// Gets the current download speed as a formatted string (e.g., "1.5 MB/s").
        /// </summary>
        public string SpeedText
        {
            get
            {
                if (SpeedBytesPerSecond <= 0) return "--";
                if (SpeedBytesPerSecond >= 1024 * 1024)
                    return $"{SpeedBytesPerSecond / (1024 * 1024):F1} MB/s";
                if (SpeedBytesPerSecond >= 1024)
                    return $"{SpeedBytesPerSecond / 1024:F1} KB/s";
                return $"{SpeedBytesPerSecond:F0} B/s";
            }
        }

        /// <summary>
        /// Gets the estimated time remaining as a formatted string (e.g., "5m 30s").
        /// </summary>
        public string TimeRemainingText
        {
            get
            {
                if (Status != DownloadStatus.Downloading || EstimatedTimeRemaining.TotalSeconds <= 0)
                    return "--";
                if (EstimatedTimeRemaining.TotalHours >= 1)
                    return $"{EstimatedTimeRemaining.Hours}h {EstimatedTimeRemaining.Minutes}m";
                if (EstimatedTimeRemaining.TotalMinutes >= 1)
                    return $"{EstimatedTimeRemaining.Minutes}m {EstimatedTimeRemaining.Seconds}s";
                return $"{EstimatedTimeRemaining.Seconds}s";
            }
        }

        /// <summary>
        /// Gets the total file size as a formatted string (e.g., "1.25 GB").
        /// </summary>
        public string SizeText
        {
            get
            {
                if (TotalBytes <= 0) return "--";
                if (TotalBytes >= 1024L * 1024 * 1024)
                    return $"{TotalBytes / (1024.0 * 1024 * 1024):F2} GB";
                if (TotalBytes >= 1024L * 1024)
                    return $"{TotalBytes / (1024.0 * 1024):F1} MB";
                if (TotalBytes >= 1024)
                    return $"{TotalBytes / 1024.0:F1} KB";
                return $"{TotalBytes} B";
            }
        }

        /// <summary>
        /// Gets the localized status text for the current download status.
        /// </summary>
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case DownloadStatus.Pending:
                        return LocaleManager.Get("dm.status.pending");
                    case DownloadStatus.Downloading:
                        return LocaleManager.Get("dm.status.downloading");
                    case DownloadStatus.Paused:
                        return LocaleManager.Get("dm.status.paused");
                    case DownloadStatus.Completed:
                        return LocaleManager.Get("dm.status.completed");
                    case DownloadStatus.Failed:
                        return LocaleManager.Get("dm.status.failed");
                    case DownloadStatus.Cancelled:
                        return LocaleManager.Get("dm.status.cancelled");
                    case DownloadStatus.WaitingInstall:
                        return LocaleManager.Get("gm.status.installing");
                    case DownloadStatus.WaitingUninstall:
                        return LocaleManager.Get("gm.status.pending_uninstall");
                    case DownloadStatus.Installed:
                        return LocaleManager.Get("gm.status.installed");
                    default:
                        return LocaleManager.Get("dm.status.unknown");
                }
            }
        }
    }

    /// <summary>
    /// Enumerates the possible states of a download.
    /// </summary>
    public enum DownloadStatus
    {
        /// <summary>
        /// The download is queued and waiting to start.
        /// </summary>
        Pending,

        /// <summary>
        /// The download is actively in progress.
        /// </summary>
        Downloading,

        /// <summary>
        /// The download has been paused by the user.
        /// </summary>
        Paused,

        /// <summary>
        /// The download has completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The download has failed due to an error.
        /// </summary>
        Failed,

        /// <summary>
        /// The download has been cancelled by the user.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The download is complete and waiting to be installed.
        /// </summary>
        WaitingInstall,

        /// <summary>
        /// The game is waiting to be uninstalled.
        /// </summary>
        WaitingUninstall,

        /// <summary>
        /// The game has been successfully installed.
        /// </summary>
        Installed
    }
}
