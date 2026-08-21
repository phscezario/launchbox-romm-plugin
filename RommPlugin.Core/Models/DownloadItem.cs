using System;
using RommPlugin.Core.Locale;

namespace RommPlugin.Core.Models
{
    public class DownloadItem
    {
        public int GameId { get; set; }
        public string GameName { get; set; }
        public string FsName { get; set; }
        public string FsPath { get; set; }
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public DownloadStatus Status { get; set; }
        public string FilePath { get; set; }
        public string PartFilePath { get; set; }
        public double SpeedBytesPerSecond { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public int RetryCount { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Error { get; set; }

        internal long _lastBytesReceived;
        internal DateTime _lastUpdateTime;

        public int Percentage
        {
            get
            {
                if (TotalBytes <= 0) return 0;
                return (int)Math.Min(100, (BytesReceived * 100) / TotalBytes);
            }
        }

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

    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Paused,
        Completed,
        Failed,
        WaitingInstall,
        WaitingUninstall,
        Installed
    }
}
