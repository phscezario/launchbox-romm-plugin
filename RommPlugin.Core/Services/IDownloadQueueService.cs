using System;
using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Manages a queue of download items with support for concurrent downloads,
    /// progress tracking, and persistent state.
    /// </summary>
    public interface IDownloadQueueService : IDisposable
    {
        /// <summary>
        /// Raised when the state of any download item changes (e.g., status transition, error).
        /// </summary>
        event Action<DownloadItem> ItemStateChanged;

        /// <summary>
        /// Raised when all items in the queue have reached a terminal state
        /// (completed, waiting for install, installed, or failed).
        /// </summary>
        event Action AllDownloadsCompleted;

        /// <summary>
        /// Raised periodically during an active download to report progress updates.
        /// </summary>
        event Action<DownloadItem> ProgressChanged;

        /// <summary>
        /// Gets a snapshot of all download items currently in the queue.
        /// </summary>
        IReadOnlyList<DownloadItem> Items { get; }

        /// <summary>
        /// Gets the number of items currently being downloaded.
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// Gets the number of items waiting to start downloading.
        /// </summary>
        int PendingCount { get; }

        /// <summary>
        /// Configures the HTTP client used for download requests.
        /// </summary>
        /// <param name="baseUrl">The base URL of the RomM server.</param>
        /// <param name="token">Optional API token for Bearer authentication.</param>
        /// <param name="username">Optional username for Basic authentication.</param>
        /// <param name="password">Optional password for Basic authentication.</param>
        void SetAuthentication(string baseUrl, string token = null, string username = null, string password = null);

        /// <summary>
        /// Adds a game to the download queue. If the game is already queued in a non-terminal
        /// state, the request is ignored. Previously failed or installed entries for the same
        /// game are removed before enqueueing.
        /// </summary>
        /// <param name="gameId">The RomM game ID.</param>
        /// <param name="gameName">Display name of the game.</param>
        /// <param name="fsName">The filename on the remote filesystem.</param>
        /// <param name="fsPath">The remote directory path containing the file.</param>
        void Enqueue(int gameId, string gameName, string fsName, string fsPath);

        /// <summary>
        /// Starts downloading the next pending items up to the configured concurrency limit.
        /// </summary>
        void StartNext();

        /// <summary>
        /// Cancels a download and removes it from the queue. Items that are already
        /// completed or waiting for install cannot be cancelled.
        /// </summary>
        /// <param name="gameId">The RomM game ID to cancel.</param>
        void Cancel(int gameId);

        /// <summary>
        /// Cancels all active and pending downloads in the queue.
        /// </summary>
        void CancelAll();

        /// <summary>
        /// Removes all items that have completed, been installed, or failed from the queue
        /// and cleans up any associated temporary files.
        /// </summary>
        void ClearCompleted();

        /// <summary>
        /// Marks a downloaded item as successfully installed.
        /// </summary>
        /// <param name="gameId">The RomM game ID of the item to mark as installed.</param>
        void InstallPending(int gameId);

        /// <summary>
        /// Marks a downloaded item as having failed during installation.
        /// </summary>
        /// <param name="gameId">The RomM game ID of the item.</param>
        /// <param name="error">Description of the installation failure.</param>
        void MarkInstallFailed(int gameId, string error);

        /// <summary>
        /// Retries a failed installation by resetting the item to the waiting-for-install state.
        /// </summary>
        /// <param name="gameId">The RomM game ID to retry installation for.</param>
        void RetryInstall(int gameId);

        /// <summary>
        /// Marks all items currently in the waiting-for-install state as installed.
        /// </summary>
        void InstallAllPending();

        /// <summary>
        /// Retries a failed download by resetting it to the pending state and re-queuing it.
        /// </summary>
        /// <param name="gameId">The RomM game ID to retry downloading.</param>
        void Retry(int gameId);

        /// <summary>
        /// Persists the current download queue state to disk.
        /// </summary>
        void SaveState();

        /// <summary>
        /// Loads the download queue state from disk, restoring any pending or
        /// waiting-for-install items.
        /// </summary>
        void LoadState();
    }
}
