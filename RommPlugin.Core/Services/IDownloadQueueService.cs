using System;
using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    public interface IDownloadQueueService : IDisposable
    {
        event Action<DownloadItem> ItemStateChanged;
        event Action AllDownloadsCompleted;
        event Action<DownloadItem> ProgressChanged;
        IReadOnlyList<DownloadItem> Items { get; }
        int ActiveCount { get; }
        int PendingCount { get; }
        void SetAuthentication(string baseUrl, string token = null, string username = null, string password = null);
        void Enqueue(int gameId, string gameName, string fsName, string fsPath);
        void StartNext();
        void Cancel(int gameId);
        void CancelAll();
        void ClearCompleted();
        void InstallPending(int gameId);
        void MarkInstallFailed(int gameId, string error);
        void RetryInstall(int gameId);
        void InstallAllPending();
        void Retry(int gameId);
        void SaveState();
        void LoadState();
    }
}
