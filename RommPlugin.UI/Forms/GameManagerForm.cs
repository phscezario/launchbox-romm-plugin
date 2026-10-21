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
        private int _isProcessing;

        /// <summary>
        /// Games already logged as "record not visible yet" by the reconcile, to
        /// avoid one line every two seconds while LaunchBox refreshes its data.
        /// </summary>
        private readonly HashSet<int> _reconcileWaitingLogged = new HashSet<int>();
        private bool _isUninstalling;
        private int _installRetryCount;
        private int _reconcileTick;
        private Func<Task> _onApplyPending;
        private Func<bool> _lastApplyWasCancelled;

        /// <summary>
        /// Consecutive install passes that changed nothing while items were still
        /// waiting to install. Those passes were skipped (the single-flight lock
        /// was held by a run that had not finished) or read a stale state file, so
        /// the trigger must be retried instead of dropped.
        /// </summary>
        private const int MaxInstallRetries = 3;

        /// <summary>Delay before retrying a skipped install pass.</summary>
        private static readonly TimeSpan InstallRetryDelay = TimeSpan.FromSeconds(10);

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
        /// <param name="lastRunWasCancelled">
        /// Optional probe for whether the last install run was cancelled by the
        /// user, so a cancel does not immediately reopen the progress window.
        /// </param>
        public void SetApplyPendingHandler(Func<Task> handler, Func<bool> lastRunWasCancelled = null)
        {
            _onApplyPending = handler;
            _lastApplyWasCancelled = lastRunWasCancelled;
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

            if (_onApplyPending != null && HasWaitingInstallInStateFile())
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
                Volatile.Read(ref _isProcessing) != 0 || _isUninstalling);

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

            // The queue is a DI singleton that outlives this form. Leaving these
            // subscribed kept every closed form alive and let the handlers run on
            // the download thread after the handle was gone (InvokeRequired is
            // false then, so they touched the grid directly and could fail the
            // download that raised the event).
            _queueService.ProgressChanged -= OnProgressChanged;
            _queueService.AllDownloadsCompleted -= OnAllDownloadsCompleted;

            _queueService.SaveState();
            _installedService.RemoveUninstalled();
            _queueWatcher?.Dispose();
            _uiTimer.Elapsed -= OnUiTimerElapsed;
            _uiTimer.Dispose();
            try { _mutex?.ReleaseMutex(); }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to release mutex: {ex.Message}");
            }
            finally { _mutex?.Dispose(); }
        }

        private void OnQueueActionDetected(QueueAction action)
        {
            if (IsDisposed || Disposing || _formClosing || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<QueueAction>(OnQueueActionDetected), action);
                }
                catch (Exception)
                {
                }
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
                    // Route through the same path as every other trigger: calling
                    // the install service directly here skipped InstallPending and
                    // the installed-games registration, so a game that finished
                    // downloading while the manager was closing got extracted but
                    // stayed "Installing" forever with no record.
                    //
                    // A cancel is a user decision here too: do not reopen the
                    // window the user just closed.
                    if (_lastApplyWasCancelled != null && _lastApplyWasCancelled())
                        return;

                    var _ = AutoInstallAsync();
                }
                return;
            }

            if (InvokeRequired)
            {
                if (IsDisposed || !IsHandleCreated) return;

                try
                {
                    BeginInvoke(new Action<DownloadItem>(OnItemStateChanged), item);
                }
                catch (Exception)
                {
                    // Handle destroyed while closing.
                }
                return;
            }

            RefreshList();

            if (item != null && item.Status == DownloadStatus.WaitingInstall && _onApplyPending != null)
            {
                // Fresh external trigger: restart the bounded retry series, or a
                // few no-progress runs earlier in the session would disable
                // retries for good.
                _installRetryCount = 0;
                var _ = AutoInstallAsync();
            }
        }

        private async Task AutoInstallAsync()
        {
            // Interlocked: a download finishing on its worker thread and the UI
            // can enter together. Two passes would open two progress windows and
            // one would sit waiting on the install lock for nothing.
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0) return;
            var changed = false;
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
                Interlocked.Exchange(ref _isProcessing, 0);

                if (!_formClosing && !IsDisposed && _onApplyPending != null)
                {
                    // The shared state file is the only process-agnostic view of
                    // pending work: another LaunchBox instance may have settled
                    // items this process downloaded, and new downloads may have
                    // completed while this pass was extracting. Reading memory
                    // here is what left games stuck on "Installing" with the
                    // archive intact on disk.
                    var stillWaiting = HasWaitingInstallInStateFile();

                    // A cancel is a user decision: do not reopen the window.
                    var runCancelled = _lastApplyWasCancelled != null && _lastApplyWasCancelled();

                    if (changed)
                    {
                        // Something moved: chain immediately. Each pass takes at
                        // least one item out of WaitingInstall, so this drains
                        // the backlog and terminates.
                        _installRetryCount = 0;
                        if (stillWaiting && !runCancelled)
                        {
                            RommLogger.Log("[GameManager] Pending installs remain in the shared state file; draining");
                            var _ = AutoInstallAsync();
                        }
                    }
                    else if (stillWaiting && !runCancelled && _installRetryCount < MaxInstallRetries)
                    {
                        // Nothing moved while items are still waiting to install:
                        // the pass could not read the state file or the service
                        // refused to run. Retry after a pause instead of dropping
                        // the trigger. Re-entering on "still WaitingInstall"
                        // alone must never happen without the delay, or this
                        // recursed until the stack overflowed.
                        _installRetryCount++;
                        RommLogger.Log($"[GameManager] Install pass made no progress with pending items; retry {_installRetryCount}/{MaxInstallRetries}");
                        RetryInstallPassLater();
                    }
                }
            }
        }

        /// <summary>
        /// Retries a skipped install pass once, after a pause. Bounded by
        /// <see cref="MaxInstallRetries"/> so a permanently stalled status does
        /// not spin; the next external trigger (a new download finishing, or the
        /// manager being reopened) starts a fresh series.
        /// </summary>
        private async void RetryInstallPassLater()
        {
            try
            {
                await Task.Delay(InstallRetryDelay);
            }
            catch
            {
                return;
            }

            if (IsDisposed || _formClosing || _onApplyPending == null) return;

            // The user cancelled the last run: do not reopen the window it just
            // closed, even on the scheduled retry.
            if (_lastApplyWasCancelled != null && _lastApplyWasCancelled()) return;

            if (!HasWaitingInstallInStateFile()) return;

            var _ = AutoInstallAsync();
        }

        /// <summary>
        /// True when the shared state file still lists items waiting to install.
        /// Used instead of the in-memory queue so a trigger is never dropped
        /// because another instance owned the item.
        /// </summary>
        private bool HasWaitingInstallInStateFile()
        {
            return GetDownloadStateMap().Values
                .Any(i => i.Status == DownloadStatus.WaitingInstall);
        }

        private Dictionary<int, DownloadItem> GetDownloadStateMap()
        {
            var statePath = RommPaths.DownloadStateFile;
            var map = new Dictionary<int, DownloadItem>();

            // A transient empty parse must be retried like a thrown read: the
            // callers treat an empty map as "nothing pending", which is the
            // dropped-trigger bug this whole path exists to avoid.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (!File.Exists(statePath)) return map;

                    var json = File.ReadAllText(statePath);
                    var state = JsonConvert.DeserializeObject<DownloadState>(json);
                    if (state == null) throw new InvalidDataException("download-state.json parsed as null");

                    foreach (var item in state.Items ?? new List<DownloadItem>())
                    {
                        if (item != null)
                            map[item.GameId] = item;
                    }

                    return map;
                }
                catch
                {
                    map.Clear();
                    if (attempt == 2) return map;
                    Thread.Sleep(50);
                }
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
                    .Where(i => i != null && i.Status == DownloadStatus.WaitingUninstall)
                    .Select(i => i.GameId));
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        private void OnProgressChanged(DownloadItem item)
        {
            if (IsDisposed || Disposing || _formClosing || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<DownloadItem>(OnProgressChanged), item);
                }
                catch (Exception)
                {
                    // Handle destroyed between the check and the post.
                }
                return;
            }

            UpdateProgressInList(item);
            UpdateSummary();
        }

        private void OnAllDownloadsCompleted()
        {
            if (IsDisposed || Disposing || _formClosing || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(OnAllDownloadsCompleted));
                }
                catch (Exception)
                {
                }
                return;
            }

            UpdateSummary();
        }

        private void OnUiTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (IsDisposed || Disposing || _formClosing || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnUiTimerElapsed(sender, e)));
                }
                catch (Exception)
                {
                    // Handle destroyed between the check and the post (closing):
                    // nothing to update, and an escaped exception here would take
                    // the process down.
                }
                return;
            }

            UpdateSummary();

            // Cheap periodic reconcile (~2 s): the shared state file is the only
            // cross-process view of installs another instance finished.
            if (++_reconcileTick < 2) return;
            _reconcileTick = 0;
            ReconcileFromStateFile();
        }

        /// <summary>
        /// Applies verdicts another LaunchBox instance already wrote to the
        /// shared state file, so a row that shows "Installing" here becomes
        /// Installed/Failed with its installed-games record in place.
        /// </summary>
        private void ReconcileFromStateFile()
        {
            if (_formClosing || IsDisposed || Volatile.Read(ref _isProcessing) != 0) return;

            try
            {
                var map = GetDownloadStateMap();
                if (map.Count == 0) return;

                var actions = InstallStateReconciler.Decide(_queueService.Items, map);
                if (actions.Count == 0) return;

                var settings = RommPluginStorage.Load();

                foreach (var action in actions)
                {
                    if (action.TargetStatus == DownloadStatus.Installed)
                    {
                        // Register first: marking the row Installed without a
                        // record leaves a queue-only row that LoadState drops on
                        // the next open, with no Retry button.
                        RegisterInstalledGame(action.GameId, settings);

                        if (_installedService.IsInstalled(action.GameId))
                        {
                            _queueService.InstallPending(action.GameId);
                            RommLogger.Log($"[GameManager] Reconciled game {action.GameId} as Installed (settled by another LaunchBox instance)");
                        }
                        else if (_gateway.FindByRommId(action.GameId) == null)
                        {
                            // The other instance's record is not visible to this
                            // one yet (LaunchBox data refresh). Writing Failed now
                            // would outrank the Installed verdict on disk forever,
                            // and the next Retry would fail with "ROM file not
                            // found". Leave it waiting: the 2 s tick retries.
                            if (_reconcileWaitingLogged.Add(action.GameId))
                            {
                                RommLogger.Log($"[GameManager] Reconciled game {action.GameId}: installed record not visible yet, will retry");
                            }
                        }
                        else
                        {
                            _queueService.MarkInstallFailed(action.GameId, "Install completed but the LaunchBox record could not be created");
                            RommLogger.LogError($"[GameManager] Reconciled game {action.GameId}: installed record missing, kept as Failed");
                        }
                    }
                    else
                    {
                        _queueService.MarkInstallFailed(action.GameId, action.Error ?? "Install failed (check log)");
                        RommLogger.Log($"[GameManager] Reconciled game {action.GameId} as Failed (settled by another LaunchBox instance)");
                    }
                }

                RefreshList();
            }
            catch (Exception ex)
            {
                // Runs from a dispatched timer tick: an exception here would be
                // unhandled and kill LaunchBox.
                RommLogger.LogException(ex);
            }
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
        /// Returns a record suitable for an uninstall. An active record is used
        /// as-is; otherwise a VALID install found on disk is resolved into a
        /// transient record. Nothing is persisted here: only the uninstall flow
        /// writes records, so a cancelled or failed uninstall can never leave a
        /// ghost "installed" entry behind (which used to appear before the user
        /// even confirmed). Returns null when there is nothing installed to
        /// delete — callers must not log success in that case.
        /// </summary>
        private InstalledGameRecord AdoptRecordIfMissing(int gameId)
        {
            var record = _installedService.GetByGameId(gameId);
            if (record != null && !record.UninstalledAt.HasValue)
                return record;

            var settings = RommPluginStorage.Load();
            if (settings == null) return null;

            var snapshot = _gateway.FindByRommId(gameId);
            if (snapshot == null) return null;

            var isFolderGame = InstallValidation.IsFolderGameEffective(
                snapshot.FolderFlagValue == bool.TrueString, settings.RomsPath, snapshot.RemotePath, snapshot.FileName);
            var installedPath = InstallValidation.ResolveInstalledPath(
                settings.RomsPath, snapshot.RemotePath, snapshot.FileName, isFolderGame);

            bool valid = isFolderGame
                ? InstallValidation.IsFolderInstallValid(installedPath)
                : InstallValidation.IsFileInstallValid(installedPath);
            if (!valid)
                return null;

            return new InstalledGameRecord
            {
                RommGameId = gameId,
                Title = snapshot.Title,
                Platform = snapshot.Platform,
                Category = snapshot.Genre,
                RemotePath = snapshot.RemotePath,
                FileName = snapshot.FileName,
                InstalledPath = installedPath,
                InstalledAt = DateTime.UtcNow
            };
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
                var state = ReadDownloadStateFile(statePath);
                var pendingItems = state?.Items?
                    .Where(i => i != null && i.Status == DownloadStatus.WaitingUninstall)
                    .ToList();

                if (pendingItems == null || pendingItems.Count == 0) return;

                // Only the rows this run settles are written at the end, applied
                // to a fresh read: deleting files takes seconds, and rewriting the
                // initial snapshot would roll back a verdict (or a row) the install
                // path committed meanwhile.
                var settled = new Dictionary<int, DownloadItem>();
                var removed = new HashSet<int>();

                foreach (var item in pendingItems)
                {
                    var record = AdoptRecordIfMissing(item.GameId);
                    if (record == null)
                    {
                        RommLogger.Log($"[Uninstall] Nothing installed to delete for GameId={item.GameId}, dropping request");
                        // Drop it through the queue too: its 5 s autosave would
                        // otherwise rewrite the WaitingUninstall row and the
                        // request would be processed again on the next open.
                        _queueService.Remove(item.GameId);
                        continue;
                    }

                    var outcome = _uninstaller.Uninstall(record, RommPluginStorage.Load().RomsPath);
                    if (outcome == GamePathDeleteOutcome.Failed)
                    {
                        RommLogger.LogError($"[Uninstall] Files could not be deleted for GameId={item.GameId}, leaving uninstall pending");
                        continue;
                    }

                    MarkUninstallSettled(item, settled);
                    // Drop from the persisted queue as well, or the save below
                    // would resurrect the row on next load.
                    removed.Add(item.GameId);
                }

                if (settled.Count == 0)
                {
                    _gateway.Save();
                    return;
                }

                var fresh = ReadDownloadStateFile(statePath);
                if (fresh?.Items == null)
                {
                    RommLogger.LogError("[Uninstall] download-state.json is unreadable; leaving the file untouched (rows stay settled in memory)");
                }
                else
                {
                    fresh.Items.RemoveAll(i => i != null && removed.Contains(i.GameId));

                    foreach (var item in fresh.Items)
                    {
                        if (item == null) continue;
                        if (!settled.TryGetValue(item.GameId, out var updated)) continue;

                        // A newer change (queue/other instance) wins over this
                        // snapshot's verdict.
                        if (item.StatusUpdatedAt > updated.StatusUpdatedAt) continue;

                        item.Status = updated.Status;
                        item.CompletedAt = updated.CompletedAt;
                        item.StatusUpdatedAt = updated.StatusUpdatedAt;
                    }

                    SafeFileWriter.WriteAllText(statePath, JsonConvert.SerializeObject(fresh, Formatting.Indented));
                }

                _gateway.Save();
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }

        private static void MarkUninstallSettled(DownloadItem item, IDictionary<int, DownloadItem> settled)
        {
            item.Status = DownloadStatus.Completed;
            item.CompletedAt = DateTime.UtcNow;
            item.StatusUpdatedAt = DateTime.UtcNow;
            settled[item.GameId] = item;
        }

        private static DownloadState ReadDownloadStateFile(string statePath)
        {
            try
            {
                if (!File.Exists(statePath)) return null;

                var json = File.ReadAllText(statePath);
                return JsonConvert.DeserializeObject<DownloadState>(json);
            }
            catch
            {
                return null;
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
