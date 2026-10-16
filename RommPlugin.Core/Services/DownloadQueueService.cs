using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Manages a queue of download items with support for concurrent downloads,
    /// progress tracking, resume capability, and persistent state.
    /// </summary>
    public class DownloadQueueService : IDownloadQueueService
    {
        private readonly SemaphoreSlim _semaphore;
        private HttpClient _http;
        private readonly List<DownloadItem> _items;
        private readonly string _stateFilePath;
        private readonly string _romsPath;
        private readonly object _lock = new object();
        private CancellationTokenSource _cts;

        /// <summary>
        /// No data received for this long aborts the download as stalled.
        /// Applies per read chunk (sliding), so slow connections survive.
        /// </summary>
        private static readonly TimeSpan StallTimeout = TimeSpan.FromMinutes(5);

        /// <inheritdoc/>
        public event Action<DownloadItem> ItemStateChanged;

        /// <inheritdoc/>
        public event Action AllDownloadsCompleted;

        /// <inheritdoc/>
        public event Action<DownloadItem> ProgressChanged;

        /// <inheritdoc/>
        public IReadOnlyList<DownloadItem> Items
        {
            get { lock (_lock) return _items.ToList(); }
        }

        /// <inheritdoc/>
        public int ActiveCount
        {
            get { lock (_lock) return _items.Count(i => i.Status == DownloadStatus.Downloading); }
        }

        /// <inheritdoc/>
        public int PendingCount
        {
            get { lock (_lock) return _items.Count(i => i.Status == DownloadStatus.Pending); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadQueueService"/> class.
        /// </summary>
        /// <param name="stateFilePath">Path to the file used to persist queue state.</param>
        /// <param name="romsPath">Local directory where downloaded ROM files are stored.</param>
        /// <param name="rommBaseUrl">Base URL of the RomM server.</param>
        /// <param name="concurrentLimit">Maximum number of simultaneous downloads. Defaults to 5.</param>
        public DownloadQueueService(string stateFilePath, string romsPath, string rommBaseUrl, int concurrentLimit = 5)
        {
            _stateFilePath = stateFilePath;
            _romsPath = romsPath;
            _semaphore = new SemaphoreSlim(concurrentLimit, concurrentLimit);
            _items = new List<DownloadItem>();
            _cts = new CancellationTokenSource();

            _http = new HttpClient
            {
                BaseAddress = new Uri(rommBaseUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        /// <inheritdoc/>
        public void SetAuthentication(string baseUrl, string token = null, string username = null, string password = null)
        {
            _http?.Dispose();
            _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = Timeout.InfiniteTimeSpan };
            AuthHeaderHelper.ApplyAuthentication(_http, token, username, password);
        }

        /// <inheritdoc/>
        public void Enqueue(int gameId, string gameName, string fsName, string fsPath)
        {
            DownloadItem item = null;
            lock (_lock)
            {
                _items.RemoveAll(i => i.GameId == gameId &&
                    (i.Status == DownloadStatus.Failed ||
                     i.Status == DownloadStatus.Installed));

                if (_items.Any(i => i.GameId == gameId &&
                    (i.Status == DownloadStatus.Downloading ||
                     i.Status == DownloadStatus.Pending ||
                     i.Status == DownloadStatus.WaitingInstall ||
                     i.Status == DownloadStatus.Completed ||
                     i.Status == DownloadStatus.Cancelled)))
                {
                    return;
                }

                var localDir = Path.Combine(
                    _romsPath,
                    RommConstants.RomsSubfolder,
                    fsPath?.Replace("/", "\\") ?? "");

                Directory.CreateDirectory(localDir);

                var localFile = Path.Combine(localDir, fsName);

                item = new DownloadItem
                {
                    GameId = gameId,
                    GameName = gameName,
                    FsName = fsName,
                    FsPath = fsPath,
                    FilePath = localFile,
                    PartFilePath = localFile + ".part",
                    Status = DownloadStatus.Pending,
                    AddedAt = DateTime.UtcNow,
                    Cts = new CancellationTokenSource()
                };

                _items.Add(item);
            }

            ItemStateChanged?.Invoke(item);
            StartNext();
        }

        /// <inheritdoc/>
        public void StartNext()
        {
            List<DownloadItem> toStart;
            lock (_lock)
            {
                toStart = _items
                    .Where(i => i.Status == DownloadStatus.Pending)
                    .Take(_semaphore.CurrentCount)
                    .ToList();
            }

            foreach (var item in toStart)
            {
                StartDownload(item);
            }
        }

        private void StartDownload(DownloadItem item)
        {
            Task.Run(async () =>
            {
                await _semaphore.WaitAsync(_cts.Token);
                try
                {
                    item.Status = DownloadStatus.Downloading;
                    ItemStateChanged?.Invoke(item);
                    await DownloadWithResumeAsync(item, item.Cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // User cancel deletes the .part; a stall timeout (fired from
                    // the linked token below) keeps it for resume and fails cleanly.
                    bool userCancel = false;
                    try { userCancel = item.Cts?.IsCancellationRequested == true; } catch { }

                    if (!userCancel)
                    {
                        item.Status = DownloadStatus.Failed;
                        item.Error = "Download stalled (no data received)";
                        ItemStateChanged?.Invoke(item);
                    }
                    else
                    {
                        try { if (File.Exists(item.PartFilePath)) File.Delete(item.PartFilePath); }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    // Keep the .part file: only user cancel, corrupt archives and
                    // uninstall/clear flows delete it, so the next retry resumes
                    // instead of restarting large downloads from zero.
                    item.Status = DownloadStatus.Failed;
                    item.Error = ex.Message;
                    ItemStateChanged?.Invoke(item);
                }
                finally
                {
                    _semaphore.Release();
                    StartNext();
                    CheckAllCompleted();
                }
            });
        }

        private async Task DownloadWithResumeAsync(DownloadItem item, CancellationToken ct)
        {
            long existingBytes = 0;
            if (File.Exists(item.PartFilePath))
            {
                existingBytes = new FileInfo(item.PartFilePath).Length;
            }

            FileMode mode;
            long totalBytes;

            using (var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/roms/download?rom_ids={item.GameId}"))
            {
                if (existingBytes > 0)
                {
                    request.Headers.Range = new RangeHeaderValue(existingBytes, null);
                }

                using (var response = await _http.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    RommLogger.Log($"[Download] Game {item.GameId} '{item.GameName}': HTTP {(int)response.StatusCode} {response.StatusCode} (resuming from {existingBytes} bytes)");
                    response.EnsureSuccessStatusCode();
                    if (existingBytes > 0 && response.StatusCode != HttpStatusCode.PartialContent)
                    {
                        RommLogger.Log($"[Download] Game {item.GameId} '{item.GameName}': server ignored Range, restarting from zero");
                    }
                    if (response.StatusCode == HttpStatusCode.PartialContent)
                    {
                        mode = FileMode.Append;
                    }
                    else
                    {
                        mode = FileMode.Create;
                        existingBytes = 0;
                    }

                    totalBytes = existingBytes +
                        (response.Content.Headers.ContentLength ?? 0);

                    item.TotalBytes = totalBytes;
                    item.BytesReceived = existingBytes;
                    item._lastUpdateTime = DateTime.UtcNow;
                    item._lastBytesReceived = existingBytes;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var file = new FileStream(item.PartFilePath, mode, FileAccess.Write, FileShare.None))
                    using (var stallCts = new CancellationTokenSource())
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, stallCts.Token))
                    {
                        var buffer = new byte[RommConstants.HttpBufferSize];
                        int bytesRead;
                        var lastProgressUpdate = DateTime.UtcNow;

                        // Sliding stall timeout: a hung connection aborts even though
                        // the client has no overall timeout (large ROMs need it).
                        // Reset on every chunk; the .part file is kept for resume.
                        stallCts.CancelAfter(StallTimeout);

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token)) > 0)
                        {
                            stallCts.CancelAfter(StallTimeout);
                            await file.WriteAsync(buffer, 0, bytesRead, ct);
                            item.BytesReceived += bytesRead;

                            var now = DateTime.UtcNow;
                            if ((now - lastProgressUpdate).TotalMilliseconds > 500)
                            {
                                UpdateProgress(item);
                                ProgressChanged?.Invoke(item);
                                lastProgressUpdate = now;
                            }
                        }
                    }
                }
            }

            if (item.Status == DownloadStatus.Cancelled)
            {
                try { if (File.Exists(item.PartFilePath)) File.Delete(item.PartFilePath); }
                catch { }
                return;
            }

            var finalPath = item.FilePath;
            try
            {
                using (var fs = File.OpenRead(item.PartFilePath))
                {
                    var header = new byte[4];
                    if (fs.Read(header, 0, 4) == 4)
                    {
                        var isZipContent = header[0] == 0x50 && header[1] == 0x4B &&
                                           header[2] == 0x03 && header[3] == 0x04;
                        var hasZipExtension = finalPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

                        if (isZipContent && !hasZipExtension)
                        {
                            finalPath = finalPath + ".zip";
                        }
                        else if (!isZipContent && hasZipExtension)
                        {
                            finalPath = finalPath.Substring(0, finalPath.Length - 4);
                        }
                    }
                }
            }
            catch { }

            if (File.Exists(finalPath))
            {
                try { File.Delete(finalPath); } catch { }
            }

            File.Move(item.PartFilePath, finalPath);
            item.FilePath = finalPath;
            item.Status = DownloadStatus.WaitingInstall;
            item.CompletedAt = DateTime.UtcNow;
            item.SpeedBytesPerSecond = 0;
            item.EstimatedTimeRemaining = TimeSpan.Zero;
            SaveState();
            ItemStateChanged?.Invoke(item);
        }

        private void UpdateProgress(DownloadItem item)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - item._lastUpdateTime).TotalSeconds;

            if (elapsed > 0)
            {
                var bytesThisInterval = item.BytesReceived - item._lastBytesReceived;
                item.SpeedBytesPerSecond = bytesThisInterval / elapsed;

                if (item.SpeedBytesPerSecond > 0)
                {
                    var remainingBytes = item.TotalBytes - item.BytesReceived;
                    item.EstimatedTimeRemaining = TimeSpan.FromSeconds(
                        remainingBytes / item.SpeedBytesPerSecond);
                }
            }

            item._lastUpdateTime = now;
            item._lastBytesReceived = item.BytesReceived;
        }

        /// <inheritdoc/>
        public void Cancel(int gameId)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId);
                if (item == null) return;

                if (item.Status == DownloadStatus.WaitingInstall ||
                    item.Status == DownloadStatus.Completed)
                {
                    return;
                }

                item.Status = DownloadStatus.Cancelled;
                item.Error = null;
                item.BytesReceived = 0;
                item.TotalBytes = 0;
                item.SpeedBytesPerSecond = 0;
                item.EstimatedTimeRemaining = TimeSpan.Zero;

                try { item.Cts?.Cancel(); } catch { }

                if (File.Exists(item.PartFilePath))
                {
                    try { File.Delete(item.PartFilePath); }
                    catch { }
                }

                ItemStateChanged?.Invoke(item);
            }
            SaveState();
        }

        /// <inheritdoc/>
        public void Remove(int gameId)
        {
            DownloadItem removed = null;
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId);
                if (item == null) return;

                try { item.Cts?.Cancel(); } catch { }
                _items.Remove(item);
                removed = item;
            }

            if (removed != null
                && !string.IsNullOrEmpty(removed.PartFilePath)
                && File.Exists(removed.PartFilePath))
            {
                try { File.Delete(removed.PartFilePath); } catch { }
            }

            SaveState();
            ItemStateChanged?.Invoke(null);
        }

        /// <inheritdoc/>
        public void CancelAll()
        {
            List<int> ids;
            lock (_lock)
            {
                ids = _items.Select(i => i.GameId).ToList();
            }

            foreach (var id in ids)
            {
                Cancel(id);
            }
        }

        /// <inheritdoc/>
        public void ClearCompleted()
        {
            lock (_lock)
            {
                var toRemove = _items.Where(i =>
                    i.Status == DownloadStatus.Completed ||
                    i.Status == DownloadStatus.Installed ||
                    i.Status == DownloadStatus.Failed ||
                    i.Status == DownloadStatus.Cancelled).ToList();

                foreach (var item in toRemove)
                {
                    if (!string.IsNullOrEmpty(item.PartFilePath) && File.Exists(item.PartFilePath))
                    {
                        try { File.Delete(item.PartFilePath); } catch { }
                    }
                }

                _items.RemoveAll(i =>
                    i.Status == DownloadStatus.Completed ||
                    i.Status == DownloadStatus.Installed ||
                    i.Status == DownloadStatus.Failed ||
                    i.Status == DownloadStatus.Cancelled);
            }

            SaveState();
            ItemStateChanged?.Invoke(null);
        }

        /// <inheritdoc/>
        public void InstallPending(int gameId)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId && i.Status == DownloadStatus.WaitingInstall);
                if (item != null)
                {
                    item.Status = DownloadStatus.Installed;
                    item.CompletedAt = DateTime.UtcNow;
                    ItemStateChanged?.Invoke(item);
                }
            }
            SaveState();
        }

        /// <inheritdoc/>
        public void MarkInstallFailed(int gameId, string error)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId && i.Status == DownloadStatus.WaitingInstall);
                if (item != null)
                {
                    item.Status = DownloadStatus.Failed;
                    item.Error = error;
                    ItemStateChanged?.Invoke(item);
                }
            }
            SaveState();
        }

        /// <inheritdoc/>
        public void RetryInstall(int gameId)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId && (i.Status == DownloadStatus.Failed || i.Status == DownloadStatus.Cancelled));
                if (item != null && !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                {
                    item.Status = DownloadStatus.WaitingInstall;
                    item.Error = null;
                    item.RetryCount = 0;
                    ItemStateChanged?.Invoke(item);
                    SaveState();
                }
            }
        }

        /// <inheritdoc/>
        public void InstallAllPending()
        {
            List<DownloadItem> toInstall;
            lock (_lock)
            {
                toInstall = _items.Where(i => i.Status == DownloadStatus.WaitingInstall).ToList();
            }

            foreach (var item in toInstall)
            {
                InstallPending(item.GameId);
            }
        }

        /// <inheritdoc/>
        public void Retry(int gameId)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId && (i.Status == DownloadStatus.Failed || i.Status == DownloadStatus.Cancelled));
                if (item != null)
                {
                    // Keep any existing .part: resume continues where it stopped.
                    // A corrupt archive is handled by deleting FilePath first.
                    item.Status = DownloadStatus.Pending;
                    item.Error = null;
                    item.RetryCount = 0;
                    item.Cts?.Dispose();
                    item.Cts = new CancellationTokenSource();
                    ItemStateChanged?.Invoke(item);
                }
            }
            SaveState();
            StartNext();
        }

        private void CheckAllCompleted()
        {
            lock (_lock)
            {
                if (_items.Count > 0 && _items.All(i =>
                    i.Status == DownloadStatus.Completed ||
                    i.Status == DownloadStatus.WaitingInstall ||
                    i.Status == DownloadStatus.Installed ||
                    i.Status == DownloadStatus.Failed))
                {
                    AllDownloadsCompleted?.Invoke();
                }
            }
        }

        /// <inheritdoc/>
        public void SaveState()
        {
            try
            {
                DownloadState state;
                lock (_lock)
                {
                    state = new DownloadState
                    {
                        Items = _items.ToList(),
                        LastUpdated = DateTime.UtcNow
                    };
                }

                var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                SafeFileWriter.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to save download state: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void LoadState()
        {
            try
            {
                if (!File.Exists(_stateFilePath)) return;

                var json = File.ReadAllText(_stateFilePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                if (state?.Items == null)
                {
                    return;
                }

                lock (_lock)
                {
                    _items.Clear();
                    var seen = new Dictionary<int, DownloadItem>();

                    foreach (var item in state.Items)
                    {
                        if (item.Status == DownloadStatus.Completed ||
                            item.Status == DownloadStatus.Installed ||
                            item.Status == DownloadStatus.Cancelled)
                        {
                            continue;
                        }

                        if (item.Status == DownloadStatus.Downloading ||
                            item.Status == DownloadStatus.Pending)
                        {
                            item.Status = DownloadStatus.Pending;
                        }

                        if (seen.TryGetValue(item.GameId, out var existing))
                        {
                            if (item.Status == DownloadStatus.WaitingInstall ||
                                (item.Status == DownloadStatus.Pending &&
                                 existing.Status == DownloadStatus.Failed))
                            {
                                seen[item.GameId] = item;
                            }
                            continue;
                        }
                        seen[item.GameId] = item;
                    }

                    foreach (var item in seen.Values)
                    {
                        _items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to load download state: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = null;

            lock (_lock)
            {
                foreach (var item in _items)
                {
                    try { item.Cts?.Cancel(); } catch { }
                    try { item.Cts?.Dispose(); } catch { }
                }
            }

            _http?.Dispose();
            _semaphore?.Dispose();
        }

    }
}
