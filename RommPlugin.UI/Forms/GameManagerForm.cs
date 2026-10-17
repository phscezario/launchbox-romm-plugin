using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Newtonsoft.Json;
using RommPlugin.UI.Helpers;
using RommPlugin.Core;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using Timer = System.Timers.Timer;

namespace RommPlugin.UI.Forms
{
    /// <summary>
    /// A form that manages game downloads, installations, and uninstalls for the Romm plugin.
    /// </summary>
    public partial class GameManagerForm : Form
    {
        private readonly DownloadQueueService _queueService;
        private readonly InstalledGamesService _installedService;
        private readonly IGameUninstaller _uninstaller;
        private readonly ILaunchBoxGameGateway _gateway;
        private readonly QueueFileWatcher _queueWatcher;
        private readonly Timer _uiTimer;
        private readonly Mutex _mutex;
        private bool _formClosing;
        private bool _isProcessing;
        private bool _isUninstalling;
        private Func<Task> _onApplyPending;

        /// <summary>
        /// Gets a value indicating whether the form initialized successfully.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameManagerForm"/> class
        /// and sets up download queue, installed games, and UI timer services.
        /// </summary>
        public GameManagerForm()
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();

            bool created;
            _mutex = new Mutex(true, "RommGame Manager_SingleInstance", out created);

            if (!created)
            {
                using (var form = new ConfirmForm("Game Manager is already running."))
                    form.ShowDialog();
                return;
            }

            try
            {
                IsInitialized = true;

                var settings = RommPluginStorage.Load();
                var pluginDir = RommPaths.PluginFolder;
                var queueFilePath = Path.Combine(pluginDir, RommConstants.DownloadQueueFile);

                _queueService = (DownloadQueueService)ServiceLocator.GetService<IDownloadQueueService>();

                _installedService = (InstalledGamesService)ServiceLocator.GetService<IInstalledGamesService>();

                _uninstaller = ServiceLocator.GetService<IGameUninstaller>();
                _gateway = ServiceLocator.GetService<ILaunchBoxGameGateway>();

                _queueService.ItemStateChanged += OnItemStateChanged;
                _queueService.ProgressChanged += OnProgressChanged;
                _queueService.AllDownloadsCompleted += OnAllDownloadsCompleted;

                _queueWatcher = new QueueFileWatcher(queueFilePath);
                _queueWatcher.ActionDetected += OnQueueActionDetected;

                _uiTimer = new Timer(1000);
                _uiTimer.Elapsed += OnUiTimerElapsed;

                Load += OnFormLoad;
                FormClosing += OnFormClosing;
                dgvGames.SelectionChanged += DgvGames_SelectionChanged;
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                RommLogger.LogException(ex);
                _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Sets the handler to be invoked when pending downloads are ready for installation.
        /// </summary>
        /// <param name="handler">The asynchronous handler to execute when applying pending items.</param>
        public void SetApplyPendingHandler(Func<Task> handler)
        {
            _onApplyPending = handler;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _queueService.LoadState();
            _installedService.Load();
            RepairInstalledFlags();
            RefreshList();
            _queueService.StartNext();
            _uiTimer.Start();
            ProcessExistingQueueFile();
            ProcessPendingUninstalls();
            _queueWatcher.Start();

            if (_onApplyPending != null && _queueService.Items.Any(i => i.Status == DownloadStatus.WaitingInstall))
            {
                var _ = AutoInstallAsync();
            }
        }

        /// <summary>
        /// Reconciles installed-games records with the LaunchBox-side fields on every
        /// open (shared runner, also executed at LaunchBox startup).
        /// </summary>
        private void RepairInstalledFlags()
        {
            InstallFlagRepairRunner.RepairAll(_installedService, _queueService);
        }

        private void ProcessExistingQueueFile()
        {
            var pluginDir = RommPaths.PluginFolder;
            var queueFilePath = Path.Combine(pluginDir, RommConstants.DownloadQueueFile);

            if (!File.Exists(queueFilePath)) return;

            try
            {
                var json = File.ReadAllText(queueFilePath);
                var actions = JsonConvert.DeserializeObject<List<QueueAction>>(json);

                if (actions != null && actions.Count > 0)
                {
                    foreach (var action in actions)
                    {
                        if (string.IsNullOrEmpty(action.Action)) continue;

                        if (action.Action == "add")
                        {
                            _queueService.Enqueue(action.GameId, action.GameName, action.FsName, action.FsPath);
                        }
                        else if (action.Action == "remove")
                        {
                            ProcessUninstallAction(action);
                        }
                    }
                    RefreshList();
                }
            }
            catch (Exception ex)
            {
                RommLogger.Log($"[QueueFile] Error processing existing queue: {ex.Message}");
            }
            finally
            {
                try { File.Delete(queueFilePath); }
                catch { }
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_formClosing) return;

            var hasActive = _queueService.Items.Any(i =>
                i.Status == DownloadStatus.Downloading ||
                i.Status == DownloadStatus.Pending ||
                i.Status == DownloadStatus.WaitingInstall ||
                _isProcessing || _isUninstalling);

            if (hasActive)
            {
                using (var form = new ConfirmForm(
                    LocaleManager.Get("gm.confirm_close_active")))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            _formClosing = true;
            _uiTimer.Stop();
            _queueWatcher.Stop();
            _queueService.SaveState();
            _installedService.RemoveUninstalled();
            _queueWatcher?.Dispose();
            try { _mutex?.ReleaseMutex(); }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to release mutex: {ex.Message}");
            }
            finally { _mutex?.Dispose(); }
        }

        private void OnQueueActionDetected(QueueAction action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<QueueAction>(OnQueueActionDetected), action);
                return;
            }

            if (action.Action == "add")
            {
                _queueService.Enqueue(action.GameId, action.GameName, action.FsName, action.FsPath);
                RefreshList();
            }
            else if (action.Action == "remove")
            {
                ProcessUninstallAction(action);
                RefreshList();
            }
        }

        private void OnItemStateChanged(DownloadItem item)
        {
            if (_formClosing)
            {
                if (item != null && item.Status == DownloadStatus.WaitingInstall && _onApplyPending != null)
                {
                    Task.Run(() => _onApplyPending());
                }
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action<DownloadItem>(OnItemStateChanged), item);
                return;
            }

            RefreshList();

            if (item != null && item.Status == DownloadStatus.WaitingInstall && _onApplyPending != null)
            {
                var _ = AutoInstallAsync();
            }
        }

        private async Task AutoInstallAsync()
        {
            if (_isProcessing) return;
            // Cross-process single-flight now lives in the install service
            // (ProcessInstallUninstallEvents), so every caller is covered.
            _isProcessing = true;
            try
            {
                var stateBefore = GetDownloadStateMap();
                await _onApplyPending();
                var stateAfter = GetDownloadStateMap();

                // Only items the batch actually marked Installed count as processed.
                // Items that left WaitingInstall for any other reason (Failed) must
                // NOT be registered as installed; untouched ones are left for the next run.
                var outcome = InstallOutcomeClassifier.Classify(stateBefore, stateAfter);
                var processedIds = outcome.ProcessedIds;
                var failedIds = outcome.FailedIds;

                var settings = RommPluginStorage.Load();
                var changed = false;

                foreach (var id in processedIds)
                {
                    _queueService.InstallPending(id);
                    var memoryItem = _queueService.Items.FirstOrDefault(i => i.GameId == id);
                    if (memoryItem != null)
                        memoryItem.CorruptArchiveAttempts = 0;
                    RegisterInstalledGame(id, settings);
                    // Scoped sweep: drop anything this install left behind that the
                    // install flow itself did not consume (stale staging dirs, etc.).
                    if (memoryItem != null)
                    {
                        RommOrphanCleanupService.CleanInstallAttempt(
                            settings.RomsPath,
                            id,
                            memoryItem.GameName,
                            null,
                            null,
                            false,
                            memoryItem.PartFilePath,
                            memoryItem.FilePath,
                            false);
                    }
                    changed = true;
                }

                foreach (var id in failedIds)
                {
                    if (stateAfter.TryGetValue(id, out var fileItem)
                        && fileItem.Error == RommConstants.CorruptArchiveMarker)
                    {
                        var memoryItem = _queueService.Items.FirstOrDefault(i => i.GameId == id);
                        var attempts = (memoryItem?.CorruptArchiveAttempts ?? 0) + 1;
                        if (memoryItem != null && !string.IsNullOrEmpty(memoryItem.FilePath))
                            RommArchiveHelper.DeleteFileWithRetry(memoryItem.FilePath);

                        if (attempts <= RommConstants.MaxCorruptArchiveRedownloads)
                        {
                            RommLogger.Log($"[GameManager] Game {id}: corrupt archive removed, re-download {attempts}/{RommConstants.MaxCorruptArchiveRedownloads}");
                            _queueService.Retry(id);
                            var retried = _queueService.Items.FirstOrDefault(i => i.GameId == id);
                            if (retried != null)
                                retried.CorruptArchiveAttempts = attempts;
                        }
                        else
                        {
                            _queueService.MarkInstallFailed(id, $"Archive corrupt after {RommConstants.MaxCorruptArchiveRedownloads} downloads (check log)");
                        }
                    }
                    else
                    {
                        _queueService.MarkInstallFailed(id, "Install failed (check log)");
                    }
                    changed = true;
                }

                // Second-chance registration: if the folder completed late and
                // the first pass found it incomplete, heal now so the row shows
                // the category without reopening the manager.
                foreach (var id in processedIds)
                {
                    if (!_installedService.IsInstalled(id))
                    {
                        RegisterInstalledGame(id, settings);
                        if (_installedService.IsInstalled(id))
                            changed = true;
                    }
                }

                if (changed)
                {
                    _queueService.SaveState();
                    if (!_formClosing && !IsDisposed)
                    {
                        BeginInvoke(new Action(RefreshList));
                    }
                }
            }
            catch (Exception ex)
            {
                RommPlugin.Core.Logging.RommLogger.LogError($"[GameManager] AutoInstall error: {ex}");
            }
            finally
            {
                _isProcessing = false;
                if (!_formClosing && !IsDisposed && _onApplyPending != null &&
                    _queueService.Items.Any(i => i.Status == DownloadStatus.WaitingInstall))
                {
                    var _ = AutoInstallAsync();
                }
            }
        }

        private Dictionary<int, DownloadItem> GetDownloadStateMap()
        {
            var statePath = RommPaths.DownloadStateFile;
            var map = new Dictionary<int, DownloadItem>();

            if (!File.Exists(statePath)) return map;

            try
            {
                var json = File.ReadAllText(statePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                foreach (var item in state?.Items ?? new List<DownloadItem>())
                {
                    if (item != null)
                        map[item.GameId] = item;
                }
            }
            catch
            {
            }

            return map;
        }

        private HashSet<int> GetPendingUninstallIds()
        {
            var statePath = RommPaths.DownloadStateFile;

            if (!File.Exists(statePath)) return new HashSet<int>();

            try
            {
                var json = File.ReadAllText(statePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                return new HashSet<int>(
                    (state?.Items ?? new List<DownloadItem>())
                    .Where(i => i.Status == DownloadStatus.WaitingUninstall)
                    .Select(i => i.GameId));
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        private void OnProgressChanged(DownloadItem item)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<DownloadItem>(OnProgressChanged), item);
                return;
            }

            UpdateProgressInList(item);
            UpdateSummary();
        }

        private void OnAllDownloadsCompleted()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnAllDownloadsCompleted));
                return;
            }

            UpdateSummary();
        }

        private void OnUiTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateSummary));
                return;
            }

            UpdateSummary();
        }

        private void RefreshList()
        {
            if (IsDisposed) return;

            int? selectedGameId = null;
            if (dgvGames.SelectedRows.Count > 0)
            {
                selectedGameId = GetSelectedGameId();
            }

            dgvGames.Rows.Clear();

            var pendingUninstalls = GetPendingUninstallIds();
            // Alphabetical, with in-progress/failed games pinned to the top.
            var rows = GameListOrdering.Order(
                GameManagerRow.Merge(_queueService.Items, _installedService.GetAll()));

            foreach (var entry in rows)
            {
                if (entry.ShowsQueueDetails)
                {
                    var item = entry.QueueItem;
                    var category = GetLastPathSegment(item.FsPath);
                    if (string.IsNullOrEmpty(category))
                    {
                        // Prefer the record's platform when one exists: the queue
                        // item may hold a stale/empty remote path.
                        var recordPlatform = entry.Installed?.Platform ?? "";
                        if (!string.IsNullOrEmpty(recordPlatform))
                        {
                            category = recordPlatform.StartsWith(RommConstants.PlatformPrefix)
                                ? recordPlatform.Substring(RommConstants.PlatformPrefix.Length)
                                : recordPlatform;
                        }
                        else
                        {
                            var lbGame = _gateway.FindByRommId(entry.GameId);
                            var platform = lbGame?.Platform ?? "";
                            category = platform.StartsWith(RommConstants.PlatformPrefix)
                                ? platform.Substring(RommConstants.PlatformPrefix.Length)
                                : platform;
                        }
                    }
                    var idx = dgvGames.Rows.Add(
                        item.GameName,
                        category,
                        item.StatusText,
                        item.Percentage,
                        item.SpeedText,
                        item.TimeRemainingText,
                        item.SizeText);

                    var row = dgvGames.Rows[idx];
                    row.Tag = entry;
                    row.DefaultCellStyle.ForeColor = GetDownloadStatusColor(item.Status);

                    if (selectedGameId.HasValue && entry.GameId == selectedGameId.Value)
                    {
                        row.Selected = true;
                    }
                }
                else
                {
                    var record = entry.Installed ?? new InstalledGameRecord
                    {
                        RommGameId = entry.GameId,
                        Title = entry.QueueItem?.GameName ?? "",
                        Platform = ""
                    };

                    var platformName = record.Platform ?? "";
                    var category = platformName.StartsWith(RommConstants.PlatformPrefix)
                        ? platformName.Substring(RommConstants.PlatformPrefix.Length)
                        : platformName;

                    var isPending = pendingUninstalls.Contains(record.RommGameId);

                    string statusText;
                    Color statusColor;

                    if (isPending)
                    {
                        statusText = LocaleManager.Get("gm.status.pending_uninstall");
                        statusColor = Color.Yellow;
                    }
                    else if (record.UninstalledAt.HasValue)
                    {
                        statusText = LocaleManager.Get("gm.status.uninstalled");
                        statusColor = Color.Gray;
                    }
                    else if (entry.QueueItem != null)
                    {
                        statusText = entry.QueueItem.StatusText;
                        statusColor = GetDownloadStatusColor(entry.QueueItem.Status);
                    }
                    else
                    {
                        statusText = LocaleManager.Get("gm.status.installed");
                        statusColor = Color.LightGreen;
                    }

                    var idx = dgvGames.Rows.Add(
                        record.Title ?? entry.DisplayName,
                        category,
                        statusText,
                        "--",
                        "--",
                        "--",
                        "--");

                    var row = dgvGames.Rows[idx];
                    row.Tag = entry;
                    row.DefaultCellStyle.ForeColor = statusColor;

                    if (selectedGameId.HasValue && entry.GameId == selectedGameId.Value)
                    {
                        row.Selected = true;
                    }
                }
            }

            UpdateSummary();
        }

        private int? GetSelectedGameId()
        {
            if (dgvGames.SelectedRows.Count == 0) return null;
            var tag = dgvGames.SelectedRows[0].Tag;
            if (tag is GameManagerRow row)
                return row.GameId;
            if (tag is DownloadItem dlItem)
                return dlItem.GameId;
            if (tag is InstalledGameRecord record)
                return record.RommGameId;
            return null;
        }

        private List<GameManagerRow> GetSelectedRows()
        {
            var result = new List<GameManagerRow>();
            foreach (DataGridViewRow gridRow in dgvGames.SelectedRows)
            {
                if (gridRow.Tag is GameManagerRow row)
                {
                    result.Add(row);
                }
                else if (gridRow.Tag is DownloadItem dlItem)
                {
                    result.Add(new GameManagerRow { GameId = dlItem.GameId, QueueItem = dlItem });
                }
                else if (gridRow.Tag is InstalledGameRecord record)
                {
                    result.Add(new GameManagerRow { GameId = record.RommGameId, Installed = record });
                }
            }
            return result;
        }

        private void UpdateProgressInList(DownloadItem item)
        {
            foreach (DataGridViewRow row in dgvGames.Rows)
            {
                if (row.Tag is GameManagerRow entry && entry.GameId == item.GameId && entry.ShowsQueueDetails)
                {
                    row.Cells[colProgress.Index].Value = item.Percentage;
                    row.Cells[colSpeed.Index].Value = item.SpeedText;
                    row.Cells[colTimeRemaining.Index].Value = item.TimeRemainingText;
                    break;
                }
            }
        }

        private string GetLastPathSegment(string fsPath)
        {
            if (string.IsNullOrWhiteSpace(fsPath)) return "";
            var parts = fsPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[parts.Length - 1] : "";
        }

        private Color GetDownloadStatusColor(DownloadStatus status)
        {
            switch (status)
            {
                case DownloadStatus.Downloading:
                    return Color.White;
                case DownloadStatus.Completed:
                    return Color.LightGreen;
                case DownloadStatus.Failed:
                    return Color.Red;
                case DownloadStatus.Cancelled:
                    return Color.Gray;
                case DownloadStatus.WaitingInstall:
                    return Color.Cyan;
                case DownloadStatus.Installed:
                    return Color.LightGreen;
                default:
                    return Color.Gray;
            }
        }

        private void UpdateSummary()
        {
            var items = _queueService.Items;
            var active = items.Count(i => i.Status == DownloadStatus.Downloading);
            var pending = items.Count(i => i.Status == DownloadStatus.Pending);
            var failed = items.Count(i => i.Status == DownloadStatus.Failed);
            var waitingInstall = items.Count(i => i.Status == DownloadStatus.WaitingInstall);
            var cancelled = items.Count(i => i.Status == DownloadStatus.Cancelled);
            var installedGames = _installedService.GetAll().Count(r => !r.UninstalledAt.HasValue);
            var totalSize = items.Where(i => i.Status == DownloadStatus.Downloading || i.Status == DownloadStatus.WaitingInstall)
                .Sum(i => i.TotalBytes);

            lblSummary.Text = $"{LocaleManager.Get("dm.active", active)} | {LocaleManager.Get("dm.pending", pending)} | {LocaleManager.Get("dm.failed", failed)} | {LocaleManager.Get("gm.status.installing")} {waitingInstall} | {LocaleManager.Get("gm.status.installed")} {installedGames} | {LocaleManager.Get("dm.cancelled", cancelled)} | {LocaleManager.Get("dm.size", FormatSize(totalSize))}";

            var selected = GetSelectedRows();
            var selectedRow = selected.FirstOrDefault();
            var selectedDownload = selectedRow?.QueueItem;
            var selectedInstalled = selectedRow?.Installed;

            btnRetry.Enabled = (selectedDownload != null && (selectedDownload.Status == DownloadStatus.Failed || selectedDownload.Status == DownloadStatus.Cancelled))
                || (selectedInstalled != null && selectedInstalled.UninstalledAt.HasValue);
            btnCancel.Enabled = items.Any(i => i.Status == DownloadStatus.Downloading || i.Status == DownloadStatus.Pending);
            btnUninstall.Enabled = selectedInstalled != null && !selectedInstalled.UninstalledAt.HasValue;
            // Always enabled: besides clearing the lists, Clear also sweeps orphan
            // artifacts (stale staging dirs, ownerless .part files), which can exist
            // even when there is nothing visible to clear. The sweep is idempotent.
            btnClear.Enabled = true;
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
            if (bytes >= 1024L * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }

        private void DgvGames_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSummary();
        }

        private void DgvGames_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var entry = dgvGames.Rows[e.RowIndex].Tag as GameManagerRow;
            if (entry == null) return;

            var item = entry.QueueItem;
            if (item != null && (item.Status == DownloadStatus.Failed || item.Status == DownloadStatus.Cancelled))
            {
                RetrySmart(item);
                return;
            }

            var installedRecord = entry.Installed;
            if (item == null && installedRecord != null && installedRecord.UninstalledAt.HasValue)
            {
                _queueService.Enqueue(installedRecord.RommGameId, installedRecord.Title, installedRecord.FileName, installedRecord.RemotePath);
                RefreshList();
            }
        }

        private void BtnRetry_Click(object sender, EventArgs e)
        {
            foreach (var entry in GetSelectedRows())
            {
                var item = entry.QueueItem;
                if (item != null && (item.Status == DownloadStatus.Failed || item.Status == DownloadStatus.Cancelled))
                {
                    RetrySmart(item);
                }
                else if (item == null && entry.Installed != null && entry.Installed.UninstalledAt.HasValue)
                {
                    var record = entry.Installed;
                    _queueService.Enqueue(record.RommGameId, record.Title, record.FileName, record.RemotePath);
                    RefreshList();
                }
            }
        }

        private void RetrySmart(DownloadItem item)
        {
            if (item == null) return;

            if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
            {
                // A corrupt zip can never succeed by reprocessing the same bytes:
                // drop it so the retry performs a full re-download instead.
                if (item.FilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                    && !RommArchiveHelper.IsZipReadable(item.FilePath))
                {
                    RommLogger.Log($"[GameManager] Game {item.GameId} '{item.GameName}': unreadable zip, re-downloading instead of reprocessing");
                    RommArchiveHelper.DeleteFileWithRetry(item.FilePath);
                    _queueService.Retry(item.GameId);
                    return;
                }

                _queueService.RetryInstall(item.GameId);
            }
            else
            {
                _queueService.Retry(item.GameId);
            }
        }

        /// <summary>
        /// Uninstalls a batch of records through the shared uninstaller, then
        /// reports the games whose files could not be removed. Preserves the
        /// Game Manager cadence: one batch dialog, one LaunchBox save, one refresh.
        /// </summary>
        private void UninstallRecords(IList<InstalledGameRecord> records)
        {
            var romsPath = RommPluginStorage.Load().RomsPath;
            var filesNotFound = new List<string>();

            foreach (var record in records)
            {
                var outcome = _uninstaller.Uninstall(record, romsPath);
                if (outcome == GamePathDeleteOutcome.Failed)
                    filesNotFound.Add(record.Title ?? record.RommGameId.ToString());
            }

            if (filesNotFound.Count > 0)
            {
                var names = string.Join("\n", filesNotFound.Take(10));
                if (filesNotFound.Count > 10)
                    names += string.Format("\n... and {0} more", filesNotFound.Count - 10);

                using (var form = new ConfirmForm(
                    string.Format(LocaleManager.Get("uninstall.files_delete_failed_batch"), filesNotFound.Count, names)))
                    form.ShowDialog();
            }

            _gateway.Save();
            RefreshList();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            foreach (var entry in GetSelectedRows())
            {
                if (entry.QueueItem != null)
                {
                    _queueService.Cancel(entry.GameId);
                }
            }
        }

        private void BtnCancelAll_Click(object sender, EventArgs e)
        {
            _queueService.CancelAll();
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            if (_isUninstalling) return;

            var selected = GetSelectedRows();
            var records = UninstallPlanDecider.ActiveRecordsInSelection(selected);

            if (records.Count == 0)
            {
                // No record (registration was skipped) but files on disk: adopt
                // valid installs so uninstall works instead of doing nothing.
                foreach (var gameId in UninstallPlanDecider.RowsWithoutRecordInSelection(selected))
                {
                    var adopted = AdoptRecordIfMissing(gameId);
                    if (adopted != null)
                        records.Add(adopted);
                }
            }

            if (records.Count == 0)
            {
                DropDeadQueueItems(selected);
                return;
            }

            _isUninstalling = true;
            try
            {
                using (var form = new ConfirmForm(
                    LocaleManager.Get("gm.confirm_uninstall", records.Count)))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                }

                UninstallRecords(records);
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
            finally
            {
                _isUninstalling = false;
            }
        }

        /// <summary>
        /// Drops stuck queue-only rows (Failed/Cancelled items with no record)
        /// so a selection without anything installed still does something
        /// visible instead of silently doing nothing. Active transfers are left
        /// alone (use Cancel for those).
        /// </summary>
        private void DropDeadQueueItems(IReadOnlyList<GameManagerRow> selected)
        {
            var drops = UninstallPlanDecider.DeadQueueDrops(selected, _queueService.Items);

            foreach (var item in drops)
            {
                _queueService.Remove(item.GameId);
                RommLogger.Log($"[GameManager] Dropped dead queue item: {item.GameName} (GameId={item.GameId}, status={item.Status})");
            }

            if (drops.Count > 0)
            {
                RefreshList();
            }
            else if (selected != null && selected.Count > 0)
            {
                using (var form = new ConfirmForm(LocaleManager.Get("uninstall.nothing_to_uninstall")))
                    form.ShowDialog();
            }
        }

        /// <summary>
        /// Returns the active record for an uninstall, adopting a valid install
        /// found on disk when no record exists (e.g. registration was skipped).
        /// Returns null when there is nothing installed to delete — callers must
        /// not log success in that case.
        /// </summary>
        private InstalledGameRecord AdoptRecordIfMissing(int gameId)
        {
            var record = _installedService.GetByGameId(gameId);
            if (record != null && !record.UninstalledAt.HasValue)
                return record;

            RegisterInstalledGame(gameId, RommPluginStorage.Load());

            record = _installedService.GetByGameId(gameId);
            if (record != null && !record.UninstalledAt.HasValue)
                return record;

            return null;
        }

        private void BtnUninstallAll_Click(object sender, EventArgs e)
        {
            if (_isUninstalling) return;

            var records = UninstallPlanDecider.ActiveRecords(_installedService.GetAll());
            if (records.Count == 0) return;

            _isUninstalling = true;
            try
            {
                using (var form = new ConfirmForm(
                    LocaleManager.Get("gm.confirm_uninstall", records.Count)))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                }

                UninstallRecords(records);
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
            finally
            {
                _isUninstalling = false;
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _queueService.ClearCompleted();
            _installedService.RemoveUninstalled();

            try
            {
                var settings = RommPluginStorage.Load();
                var knownParts = new HashSet<string>(
                    _queueService.Items
                        .Select(i => i.PartFilePath)
                        .Where(p => !string.IsNullOrEmpty(p)),
                    StringComparer.OrdinalIgnoreCase);
                var swept = RommOrphanCleanupService.CleanFull(
                    settings.RomsPath,
                    RommPaths.PluginFolder,
                    knownParts);
                if (swept.TotalRemoved > 0)
                    RommLogger.Log($"[GameManager] Clear: removed {swept.TotalRemoved} orphan(s).");
            }
            catch
            {
            }

            RefreshList();
        }

        private void ProcessUninstallAction(QueueAction action)
        {
            try
            {
                var record = AdoptRecordIfMissing(action.GameId);
                if (record == null)
                {
                    RommLogger.Log($"[Uninstall] Nothing installed to delete for {action.GameName} (GameId={action.GameId})");
                    return;
                }

                var outcome = _uninstaller.Uninstall(record, RommPluginStorage.Load().RomsPath);
                if (outcome == GamePathDeleteOutcome.Failed)
                {
                    RommLogger.LogError($"[Uninstall] Files could not be deleted for {action.GameName} (GameId={action.GameId}), leaving uninstall pending");
                    return;
                }

                _gateway.Save();
                RommLogger.Log($"[Uninstall] Completed: {action.GameName} (GameId={action.GameId})");
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }

        private void ProcessPendingUninstalls()
        {
            var statePath = RommPaths.DownloadStateFile;

            if (!File.Exists(statePath)) return;

            try
            {
                var json = File.ReadAllText(statePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                var pendingItems = state?.Items?
                    .Where(i => i.Status == DownloadStatus.WaitingUninstall)
                    .ToList();

                if (pendingItems == null || pendingItems.Count == 0) return;

                foreach (var item in pendingItems)
                {
                    var record = AdoptRecordIfMissing(item.GameId);
                    if (record == null)
                    {
                        RommLogger.Log($"[Uninstall] Nothing installed to delete for GameId={item.GameId}, dropping request");
                        item.Status = DownloadStatus.Completed;
                        item.CompletedAt = DateTime.UtcNow;
                        continue;
                    }

                    var outcome = _uninstaller.Uninstall(record, RommPluginStorage.Load().RomsPath);
                    if (outcome == GamePathDeleteOutcome.Failed)
                    {
                        RommLogger.LogError($"[Uninstall] Files could not be deleted for GameId={item.GameId}, leaving uninstall pending");
                        continue;
                    }

                    item.Status = DownloadStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
                    // Drop from the persisted queue as well, or the save below
                    // would resurrect the row on next load.
                    state.Items?.Remove(item);
                }

                var updatedJson = JsonConvert.SerializeObject(state, Formatting.Indented);
                var tempPath = Path.Combine(Path.GetDirectoryName(statePath), $"download-state.{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(tempPath, updatedJson);
                    File.Copy(tempPath, statePath, true);
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }

                _gateway.Save();
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }

        private void RegisterInstalledGame(int gameId, RommPluginSettings settings)
        {
            try
            {
                if (_installedService.IsInstalled(gameId))
                    return;

                var snapshot = _gateway.FindByRommId(gameId);
                if (snapshot == null) return;

                var isFolderGame = InstallValidation.IsFolderGameEffective(
                    snapshot.FolderFlagValue == bool.TrueString, settings.RomsPath, snapshot.RemotePath, snapshot.FileName);
                if (isFolderGame && snapshot.FolderFlagValue != bool.TrueString)
                {
                    // Disk evidence overrides the stale sync flag; write it back
                    // so every other flow sees the folder game as such.
                    _gateway.BackfillFolderFlag(gameId);
                    RommLogger.Log($"[RommPlugin] Corrected stale folder flag: {snapshot.Title} (ID: {gameId})");
                }

                var installedPath = InstallValidation.ResolveInstalledPath(
                    settings.RomsPath, snapshot.RemotePath, snapshot.FileName, isFolderGame);

                // Never register failed/partial installs: a previous error must
                // not leave the game marked as installed.
                bool valid = isFolderGame
                    ? InstallValidation.IsFolderInstallValid(installedPath)
                    : InstallValidation.IsFileInstallValid(installedPath);
                if (!valid)
                {
                    RommLogger.Log($"[RommPlugin] Not registering installed (missing or incomplete): {snapshot.Title} (ID: {gameId})");
                    return;
                }

                _installedService.MarkInstalled(new InstalledGameRecord
                {
                    RommGameId = gameId,
                    Title = snapshot.Title,
                    Platform = snapshot.Platform,
                    Category = snapshot.Genre,
                    RemotePath = snapshot.RemotePath,
                    FileName = snapshot.FileName,
                    InstalledPath = installedPath,
                    InstalledAt = DateTime.UtcNow
                });

                RommLogger.Log($"[RommPlugin] Registered installed: {snapshot.Title} (ID: {gameId})");
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }

        private void DgvGames_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colProgress.Index) return;

            var entry = dgvGames.Rows[e.RowIndex].Tag as GameManagerRow;
            var item = entry?.QueueItem;
            if (entry == null || item == null || !entry.ShowsQueueDetails)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                e.Handled = true;
                return;
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.All);

            Color barColor;
            switch (item.Status)
            {
                case DownloadStatus.Downloading:
                    barColor = Color.Crimson;
                    break;
                case DownloadStatus.WaitingInstall:
                case DownloadStatus.Completed:
                    barColor = Color.FromArgb(0, 180, 0);
                    break;
                case DownloadStatus.Failed:
                    barColor = Color.Red;
                    break;
                default:
                    barColor = Color.Gray;
                    break;
            }

            ListViewProgressRenderer.DrawProgressCell(
                e.Graphics, e.CellBounds, item.Percentage,
                barColor);

            e.Handled = true;
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("gm.title");
            btnRetry.Text = LocaleManager.Get("dm.retry");
            btnCancel.Text = LocaleManager.Get("dm.cancel");
            btnCancelAll.Text = LocaleManager.Get("dm.cancel_all");
            btnUninstall.Text = LocaleManager.Get("gm.uninstall");
            btnUninstallAll.Text = LocaleManager.Get("gm.uninstall_all");
            btnClear.Text = LocaleManager.Get("dm.clear");
            colName.HeaderText = LocaleManager.Get("dm.col_name");
            colCategory.HeaderText = LocaleManager.Get("dm.col_category");
            colStatus.HeaderText = LocaleManager.Get("dm.col_status");
            colProgress.HeaderText = LocaleManager.Get("dm.col_progress");
            colSpeed.HeaderText = LocaleManager.Get("dm.col_speed");
            colTimeRemaining.HeaderText = LocaleManager.Get("dm.col_time_remaining");
            colSize.HeaderText = LocaleManager.Get("dm.col_size");
        }


    }
}
