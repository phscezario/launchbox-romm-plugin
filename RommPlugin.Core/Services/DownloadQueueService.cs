using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Decisions;
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
        private readonly HashSet<int> _removedIds = new HashSet<int>();

        /// <summary>Serializes the read-merge-write sequence of <see cref="SaveState"/>.</summary>
        private readonly object _saveLock = new object();
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
                    StatusUpdatedAt = DateTime.UtcNow,
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
            var toStart = new List<DownloadItem>();

            lock (_lock)
            {
                // Reserve each slot synchronously: two concurrent passes must
                // never hand the same Pending item to two workers, which ends in
                // a FileShare.None clash on the .part file (looks like "one at a
                // time" and a random Failed row).
                while (_semaphore.Wait(0))
                {
                    var item = _items.FirstOrDefault(i => i.Status == DownloadStatus.Pending);
                    if (item == null)
                    {
                        _semaphore.Release();
                        break;
                    }

                    item.Status = DownloadStatus.Downloading;
                    toStart.Add(item);
                }
            }

            foreach (var item in toStart)
            {
                try
                {
                    StartDownload(item);
                }
                catch (Exception ex)
                {
                    // A reserved slot must never be stranded: without this the
                    // queue would stop starting downloads for the rest of the
                    // session. Task.Run failing is the only realistic trigger.
                    RommLogger.LogError($"[Download] Game {item.GameId} could not be started: {ex.Message}");
                    item.Status = DownloadStatus.Failed;
                    item.Error = ex.Message;
                    try { _semaphore.Release(); } catch { }
                    ItemStateChanged?.Invoke(item);
                    SaveState();
                }
            }
        }

        private void StartDownload(DownloadItem item)
        {
            // The concurrency slot was already reserved by StartNext; this task
            // owns it until it finishes and releases it exactly once.
            Task.Run(async () =>
            {
                try
                {
                    // The start notification lives inside the task, not in
                    // StartNext: a throwing subscriber must not stop the
                    // download from starting while its slot is already
                    // reserved, which would leak the slot and wedge the queue.
                    try { ItemStateChanged?.Invoke(item); }
                    catch (Exception notifyEx)
                    {
                        RommLogger.LogError($"[Download] State listener failed for game {item.GameId}: {notifyEx.Message}");
                    }

                    await DownloadWithResumeAsync(item, item.Cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // User cancel deletes the .part; a stall timeout (fired from
                    // the linked token below) keeps it for resume and fails cleanly.
                    bool userCancel = false;
                    try { userCancel = item.Cts?.IsCancellationRequested == true; } catch { }

                    if (item.PreservePartFile)
                    {
                        // The row was dropped from the queue while queued or in
                        // flight: leave the .part untouched so a later re-queue
                        // resumes instead of restarting from zero.
                        RommLogger.Log($"[Download] Game {item.GameId} '{item.GameName}': row dropped, keeping partial file for resume");
                    }
                    else if (!userCancel)
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

                    SaveState();
                }
                catch (Exception ex)
                {
                    // Keep the .part file: only user cancel, corrupt archives and
                    // uninstall/clear flows delete it, so the next retry resumes
                    // instead of restarting large downloads from zero.
                    item.Status = DownloadStatus.Failed;
                    item.Error = ex.Message;
                    ItemStateChanged?.Invoke(item);
                    SaveState();
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

                    // Prefer the server's own total (Content-Range) — RomM may
                    // stream without Content-Length, which used to leave
                    // TotalBytes at 0 and the progress bar dead at 0%.
                    totalBytes = response.Content.Headers.ContentRange?.Length
                        ?? (existingBytes + (response.Content.Headers.ContentLength ?? 0));

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
                        var lastStateSave = DateTime.UtcNow;

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

                                // Persist periodically so download-state.json
                                // tracks the real byte count instead of freezing
                                // at the value from the last status change.
                                if ((now - lastStateSave).TotalSeconds >= 5)
                                {
                                    SaveState();
                                    lastStateSave = now;
                                }
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
            var isZipContent = false;
            try
            {
                using (var fs = File.OpenRead(item.PartFilePath))
                {
                    var header = new byte[4];
                    if (fs.Read(header, 0, 4) == 4)
                    {
                        isZipContent = header[0] == 0x50 && header[1] == 0x4B &&
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

            // Integrity gate: a truncated/corrupt archive must never reach the
            // install pass as "Installing". Opening the central directory here
            // is cheap even for a multi-gigabyte ROM and catches the failure now
            // instead of minutes into an extraction. Failing with the corrupt
            // marker routes the item into the existing delete + re-download flow
            // (bounded by MaxCorruptArchiveRedownloads).
            if (!DownloadArchiveDecider.IsReleasable(finalPath, isZipContent))
            {
                RommLogger.LogError($"[Download] Game {item.GameId} '{item.GameName}': downloaded archive is unreadable (corrupt/truncated) at '{finalPath}'");
                item.Status = DownloadStatus.Failed;
                item.Error = RommConstants.CorruptArchiveMarker;
                item.CompletedAt = DateTime.UtcNow;
                item.StatusUpdatedAt = DateTime.UtcNow;
                item.SpeedBytesPerSecond = 0;
                item.EstimatedTimeRemaining = TimeSpan.Zero;
                SaveState();
                ItemStateChanged?.Invoke(item);
                return;
            }

            item.Status = DownloadStatus.WaitingInstall;
            item.CompletedAt = DateTime.UtcNow;
            item.StatusUpdatedAt = DateTime.UtcNow;
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

                // Only discard the partial file of a dead item. A queued or
                // in-flight transfer keeps its .part (and is told to preserve it)
                // so dropping the row never destroys a resumable download.
                item.PreservePartFile = item.Status == DownloadStatus.Pending
                    || item.Status == DownloadStatus.Downloading;

                try { item.Cts?.Cancel(); } catch { }
                _items.Remove(item);
                _removedIds.Add(gameId);
                removed = item;
            }

            if (removed != null
                && !removed.PreservePartFile
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
                    _removedIds.Add(item.GameId);

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
                    item.StatusUpdatedAt = DateTime.UtcNow;
                    // A row that succeeded must not keep the error of a previous
                    // attempt in the file (the UI showed a stale message).
                    item.Error = null;
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
                    item.StatusUpdatedAt = DateTime.UtcNow;
                    item.Error = error;
                    // A failure is not a completion: leaving the old timestamp
                    // made the row look settled in the UI.
                    item.CompletedAt = null;
                    ItemStateChanged?.Invoke(item);
                }
            }
            SaveState();
        }

        /// <inheritdoc/>
        public void RetryInstall(int gameId)
        {
            DownloadItem retried = null;
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.GameId == gameId && (i.Status == DownloadStatus.Failed || i.Status == DownloadStatus.Cancelled));
                if (item != null && !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                {
                    item.Status = DownloadStatus.WaitingInstall;
                    item.Error = null;
                    item.RetryCount = 0;
                    // A user Retry must beat a Failed verdict already on disk;
                    // without this stamp the merge (and the UI reconcile) would
                    // revert it a couple of seconds later.
                    item.StatusUpdatedAt = DateTime.UtcNow;
                    retried = item;
                }
            }

            if (retried == null) return;

            // Persist before announcing: the install pass this event triggers
            // reads the shared file, and with the save still pending it found
            // nothing and only retried a delay later.
            SaveState();
            ItemStateChanged?.Invoke(retried);
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
                    // Re-derive the counters from that file instead of trusting
                    // stale values from the previous attempt.
                    item.Status = DownloadStatus.Pending;
                    item.Error = null;
                    item.RetryCount = 0;
                    item.StatusUpdatedAt = DateTime.UtcNow;
                    item.BytesReceived = PartLength(item);
                    item.TotalBytes = 0;
                    item.SpeedBytesPerSecond = 0;
                    item.EstimatedTimeRemaining = TimeSpan.Zero;
                    item.Cts?.Dispose();
                    item.Cts = new CancellationTokenSource();
                    ItemStateChanged?.Invoke(item);
                }
            }
            SaveState();
            StartNext();
        }

        /// <summary>
        /// Byte count already on disk for an item's partial file. This is the
        /// only trustworthy progress value while the queue is not transferring
        /// (the in-memory counters may be stale or from another attempt).
        /// </summary>
        private static long PartLength(DownloadItem item)
        {
            try
            {
                return !string.IsNullOrEmpty(item.PartFilePath) && File.Exists(item.PartFilePath)
                    ? new FileInfo(item.PartFilePath).Length
                    : 0;
            }
            catch
            {
                return 0;
            }
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
            // Serialized: the 5 s auto-save and a UI action (Retry, Remove, ...)
            // used to interleave read -> merge -> write. The later write then
            // persisted a snapshot read before the earlier one, dropping the row
            // it had just added or re-asserting a status it had settled.
            lock (_saveLock)
            {
                SaveStateCore();
            }
        }

        private void SaveStateCore()
        {
            try
            {
                List<DownloadItem> local;
                List<int> removed;
                lock (_lock)
                {
                    local = _items.ToList();
                    removed = _removedIds.ToList();
                }

                // The state file is shared with every LaunchBox instance. Merge
                // instead of overwriting: an install verdict (Installed/Failed)
                // written by the other instance, or a download it started, must
                // not be erased by this process's snapshot.
                var onDisk = ReadStateItems();
                if (onDisk == null)
                {
                    // Unreadable right now (another writer replacing it). Keep the
                    // local state in memory and retry on the next save: writing a
                    // memory-only snapshot here would drop the other instance's
                    // rows and could resurrect or erase verdicts.
                    RommLogger.LogError("Download state file unreadable; skipping this save to preserve the rows written by the other instance.");
                    return;
                }

                var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, removed);

                var state = new DownloadState
                {
                    Items = merged,
                    LastUpdated = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                SafeFileWriter.WriteAllText(_stateFilePath, json);

                lock (_lock)
                {
                    // Only the removals this snapshot carried: a Remove that ran
                    // while the file was being written must still be saved.
                    foreach (var id in removed)
                    {
                        _removedIds.Remove(id);
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to save download state: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the current items from the shared state file. A missing file
        /// counts as empty; an unreadable one returns <c>null</c> so the caller
        /// can skip the round instead of writing a memory-only state that erases
        /// every row the other instance owns.
        /// </summary>
        private List<DownloadItem> ReadStateItems()
        {
            try
            {
                if (!File.Exists(_stateFilePath)) return new List<DownloadItem>();

                var json = File.ReadAllText(_stateFilePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                if (state == null) return null;

                // "Items": null is a valid, empty queue (not an unreadable one):
                // returning null here made SaveState skip forever.
                return state.Items ?? new List<DownloadItem>();
            }
            catch
            {
                return null;
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
                    // A download running in this process must survive a reload:
                    // clearing it orphans the live worker, and re-adding the same
                    // id as Pending starts a second worker on the same .part
                    // (FileShare.None -> instant Failed). Closing and reopening
                    // the manager during a download used to do exactly that.
                    var active = _items
                        .Where(i => i.Status == DownloadStatus.Downloading && i.Cts != null)
                        .ToList();
                    var activeIds = new HashSet<int>(active.Select(i => i.GameId));

                    _items.Clear();
                    // _removedIds is kept on purpose: a removal that has not been
                    // persisted yet must not be resurrected by this reload.
                    var seen = new Dictionary<int, DownloadItem>();

                    foreach (var live in active)
                    {
                        // Only indexed here: the loop below adds every entry of
                        // `seen` to `_items` exactly once.
                        seen[live.GameId] = live;
                    }

                    foreach (var item in state.Items)
                    {
                        if (item == null) continue;
                        if (activeIds.Contains(item.GameId)) continue;
                        // A removal whose save has not landed yet must not be
                        // resurrected by this reload.
                        if (_removedIds.Contains(item.GameId)) continue;

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
                            // Cts is [JsonIgnore]: a row restored from the file has
                            // no token, and StartNext dereferences item.Cts.Token.
                            // Without this the resumed download dies with an NRE and
                            // the row shows a random Failed.
                            if (item.Cts == null)
                                item.Cts = new CancellationTokenSource();
                            // Nothing is transferring yet: re-derive the counters
                            // from the .part on disk so a restart shows the real
                            // resume position instead of a frozen 0/0.
                            item.BytesReceived = PartLength(item);
                            item.TotalBytes = 0;
                            item.SpeedBytesPerSecond = 0;
                            item.EstimatedTimeRemaining = TimeSpan.Zero;
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
