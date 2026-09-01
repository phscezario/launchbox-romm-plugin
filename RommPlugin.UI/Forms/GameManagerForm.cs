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
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using Timer = System.Timers.Timer;

namespace RommPlugin.UI.Forms
{
    public partial class GameManagerForm : Form
    {
        private readonly DownloadQueueService _queueService;
        private readonly InstalledGamesService _installedService;
        private readonly QueueFileWatcher _queueWatcher;
        private readonly Timer _uiTimer;
        private readonly Mutex _mutex;
        private bool _formClosing;
        private bool _isProcessing;
        private bool _isUninstalling;
        private Func<Task> _onApplyPending;

        public bool IsInitialized { get; private set; }

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

        public void SetApplyPendingHandler(Func<Task> handler)
        {
            _onApplyPending = handler;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _queueService.LoadState();
            _installedService.Load();
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
            _isProcessing = true;
            try
            {
                var pendingIdsBefore = GetPendingInstallIds();
                await _onApplyPending();
                var pendingIdsAfter = GetPendingInstallIds();

                var processedIds = pendingIdsBefore.Except(pendingIdsAfter).ToList();
                var failedIds = pendingIdsBefore.Intersect(pendingIdsAfter).ToList();

                var settings = RommPluginStorage.Load();
                var changed = false;

                foreach (var id in processedIds)
                {
                    _queueService.InstallPending(id);
                    RegisterInstalledGame(id, settings);
                    changed = true;
                }

                foreach (var id in failedIds)
                {
                    _queueService.MarkInstallFailed(id, "Install failed (check log)");
                    changed = true;
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

        private HashSet<int> GetPendingInstallIds()
        {
            var statePath = RommPaths.DownloadStateFile;

            if (!File.Exists(statePath)) return new HashSet<int>();

            try
            {
                var json = File.ReadAllText(statePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                return new HashSet<int>(
                    (state?.Items ?? new List<DownloadItem>())
                    .Where(i => i.Status == DownloadStatus.WaitingInstall)
                    .Select(i => i.GameId));
            }
            catch
            {
                return new HashSet<int>();
            }
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
                var tag = dgvGames.SelectedRows[0].Tag;
                if (tag is DownloadItem dlItem)
                    selectedGameId = dlItem.GameId;
                else if (tag is InstalledGameRecord record)
                    selectedGameId = record.RommGameId;
            }

            dgvGames.Rows.Clear();

            var pendingUninstalls = GetPendingUninstallIds();

            foreach (var item in _queueService.Items)
            {
                if (item.Status == DownloadStatus.Installed)
                    continue;

                var category = GetCategory(item.FsPath);
                var idx = dgvGames.Rows.Add(
                    item.GameName,
                    category,
                    item.StatusText,
                    item.Percentage,
                    item.SpeedText,
                    item.TimeRemainingText,
                    item.SizeText);

                var row = dgvGames.Rows[idx];
                row.Tag = item;
                row.DefaultCellStyle.ForeColor = GetDownloadStatusColor(item.Status);

                if (selectedGameId.HasValue && item.GameId == selectedGameId.Value)
                {
                    row.Selected = true;
                }
            }

            foreach (var record in _installedService.GetAll())
            {
                var platformName = record.Platform ?? "";
                var category = platformName.StartsWith(RommConstants.PlatformPrefix)
                    ? platformName.Substring(7)
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
                else
                {
                    statusText = LocaleManager.Get("gm.status.installed");
                    statusColor = Color.LightGreen;
                }

                var idx = dgvGames.Rows.Add(
                    record.Title ?? "",
                    category,
                    statusText,
                    "--",
                    "--",
                    "--",
                    "--");

                var row = dgvGames.Rows[idx];
                row.Tag = record;
                row.DefaultCellStyle.ForeColor = statusColor;

                if (selectedGameId.HasValue && record.RommGameId == selectedGameId.Value)
                {
                    row.Selected = true;
                }
            }

            UpdateSummary();
        }

        private void UpdateProgressInList(DownloadItem item)
        {
            foreach (DataGridViewRow row in dgvGames.Rows)
            {
                if (row.Tag is DownloadItem existing && existing.GameId == item.GameId)
                {
                    row.Cells[colProgress.Index].Value = item.Percentage;
                    row.Cells[colSpeed.Index].Value = item.SpeedText;
                    row.Cells[colTimeRemaining.Index].Value = item.TimeRemainingText;
                    break;
                }
            }
        }

        private string GetCategory(string fsPath)
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
            var installed = items.Count(i => i.Status == DownloadStatus.Installed);
            var installedGames = _installedService.GetAll().Count(r => !r.UninstalledAt.HasValue);
            var totalSize = items.Where(i => i.Status == DownloadStatus.Downloading || i.Status == DownloadStatus.WaitingInstall)
                .Sum(i => i.TotalBytes);

            lblSummary.Text = $"{LocaleManager.Get("dm.active", active)} | {LocaleManager.Get("dm.pending", pending)} | {LocaleManager.Get("dm.failed", failed)} | {LocaleManager.Get("gm.status.installing")} {waitingInstall} | {LocaleManager.Get("gm.status.installed")} {installed + installedGames} | {LocaleManager.Get("dm.size", FormatSize(totalSize))}";

            DownloadItem selectedDownload = null;
            InstalledGameRecord selectedInstalled = null;

            if (dgvGames.SelectedRows.Count > 0)
            {
                var tag = dgvGames.SelectedRows[0].Tag;
                if (tag is DownloadItem dlItem)
                    selectedDownload = dlItem;
                else if (tag is InstalledGameRecord record)
                    selectedInstalled = record;
            }

            btnRetry.Enabled = selectedDownload != null && selectedDownload.Status == DownloadStatus.Failed;
            btnCancel.Enabled = items.Any(i => i.Status == DownloadStatus.Downloading || i.Status == DownloadStatus.Pending);
            btnUninstall.Enabled = selectedInstalled != null && !selectedInstalled.UninstalledAt.HasValue;
            btnClear.Enabled = items.Any(i => i.Status == DownloadStatus.Installed || i.Status == DownloadStatus.Failed) ||
                              _installedService.GetAll().Any(r => r.UninstalledAt.HasValue);
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

            var item = dgvGames.Rows[e.RowIndex].Tag as DownloadItem;
            if (item == null) return;

            if (item.Status == DownloadStatus.Failed)
            {
                if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    _queueService.RetryInstall(item.GameId);
                else
                    _queueService.Retry(item.GameId);
            }
        }

        private void BtnRetry_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvGames.SelectedRows)
            {
                if (row.Tag is DownloadItem item && item.Status == DownloadStatus.Failed)
                {
                    if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                        _queueService.RetryInstall(item.GameId);
                    else
                        _queueService.Retry(item.GameId);
                }
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvGames.SelectedRows)
            {
                if (row.Tag is DownloadItem item)
                {
                    _queueService.Cancel(item.GameId);
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

            var selectedRecords = dgvGames.SelectedRows.Cast<DataGridViewRow>()
                .Where(r => r.Tag is InstalledGameRecord)
                .Select(r => r.Tag as InstalledGameRecord)
                .Where(r => !r.UninstalledAt.HasValue)
                .ToList();

            if (selectedRecords.Count == 0) return;

            _isUninstalling = true;
            try
            {
                using (var form = new ConfirmForm(
                    LocaleManager.Get("gm.confirm_uninstall", selectedRecords.Count)))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                }

                var filesNotFound = new List<string>();

                foreach (var record in selectedRecords)
                {
                    var game = FindGameByRommId(record.RommGameId);
                    if (game != null)
                    {
                        ClearGameAdditionalApplications(game, record.InstalledPath);
                        game.ApplicationPath = null;
                        game.Installed = false;
                    }

                    var filesDeleted = TryDeleteGameFiles(record);

                    if (!filesDeleted)
                    {
                        filesNotFound.Add(record.Title ?? record.RommGameId.ToString());
                    }

                    _installedService.MarkUninstalled(record.RommGameId);
                }

                if (filesNotFound.Count > 0)
                {
                    var names = string.Join("\n", filesNotFound.Take(10));
                    if (filesNotFound.Count > 10)
                        names += string.Format("\n... and {0} more", filesNotFound.Count - 10);

                    using (var form = new ConfirmForm(
                        string.Format(LocaleManager.Get("uninstall.files_not_found_batch"), filesNotFound.Count, names)))
                        form.ShowDialog();
                }

                PluginHelper.DataManager.Save();
                RefreshList();
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

        private void BtnUninstallAll_Click(object sender, EventArgs e)
        {
            if (_isUninstalling) return;

            var allRecords = _installedService.GetAll()
                .Where(r => !r.UninstalledAt.HasValue)
                .ToList();

            if (allRecords.Count == 0) return;

            _isUninstalling = true;
            try
            {
                using (var form = new ConfirmForm(
                    LocaleManager.Get("gm.confirm_uninstall", allRecords.Count)))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                }

                var filesNotFound = new List<string>();

                foreach (var record in allRecords)
                {
                    var game = FindGameByRommId(record.RommGameId);
                    if (game != null)
                    {
                        ClearGameAdditionalApplications(game, record.InstalledPath);
                        game.ApplicationPath = null;
                        game.Installed = false;
                    }

                    var filesDeleted = TryDeleteGameFiles(record);

                    if (!filesDeleted)
                    {
                        filesNotFound.Add(record.Title ?? record.RommGameId.ToString());
                    }

                    _installedService.MarkUninstalled(record.RommGameId);
                }

                if (filesNotFound.Count > 0)
                {
                    var names = string.Join("\n", filesNotFound.Take(10));
                    if (filesNotFound.Count > 10)
                        names += string.Format("\n... and {0} more", filesNotFound.Count - 10);

                    using (var form = new ConfirmForm(
                        string.Format(LocaleManager.Get("uninstall.files_not_found_batch"), filesNotFound.Count, names)))
                        form.ShowDialog();
                }

                PluginHelper.DataManager.Save();
                RefreshList();
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
            RefreshList();
        }

        private void ProcessUninstallAction(QueueAction action)
        {
            try
            {
                var record = _installedService.GetByGameId(action.GameId);

                var game = FindGameByRommId(action.GameId);
                if (game != null)
                {
                    ClearGameAdditionalApplications(game, record?.InstalledPath);
                    game.ApplicationPath = null;
                    game.Installed = false;
                }

                var filesDeleted = TryDeleteGameFiles(record);
                if (!filesDeleted)
                {
                    RommLogger.Log($"[Uninstall] Files not found for {action.GameName} (GameId={action.GameId}), proceeding anyway");
                }

                _installedService.MarkUninstalled(action.GameId);
                PluginHelper.DataManager.Save();
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
                    var record = _installedService.GetByGameId(item.GameId);

                    var game = FindGameByRommId(item.GameId);
                    if (game != null)
                    {
                        ClearGameAdditionalApplications(game, record?.InstalledPath);
                        game.ApplicationPath = null;
                        game.Installed = false;
                    }

                    var filesDeleted = TryDeleteGameFiles(record);

                    if (!filesDeleted)
                    {
                    }

                    _installedService.MarkUninstalled(item.GameId);
                    item.Status = DownloadStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
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

                PluginHelper.DataManager.Save();
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

                var game = FindGameByRommId(gameId);
                if (game == null) return;

                var fields = game.GetAllCustomFields()
                    .GroupBy(f => f.Name)
                    .ToDictionary(g => g.Key, g => g.Last().Value);

                fields.TryGetValue(GameCustomFields.RemotePath, out var remotePath);
                fields.TryGetValue(GameCustomFields.FileName, out var fileName);
                fields.TryGetValue(GameCustomFields.IsFolderGame, out var folderValue);
                var isFolderGame = folderValue == bool.TrueString;

                var localFile = Path.Combine(
                    settings.RomsPath, RommConstants.RomsSubfolder,
                    (remotePath ?? "").Replace("/", "\\"),
                    fileName ?? ""
                );

                string installedPath;
                if (isFolderGame)
                {
                    installedPath = Path.Combine(
                        Path.GetDirectoryName(localFile),
                        Path.GetFileNameWithoutExtension(localFile));
                }
                else
                {
                    if (File.Exists(localFile))
                        installedPath = localFile;
                    else if (File.Exists(localFile + ".zip"))
                        installedPath = localFile + ".zip";
                    else
                        installedPath = localFile;
                }

                _installedService.MarkInstalled(new InstalledGameRecord
                {
                    RommGameId = gameId,
                    Title = game.Title,
                    Platform = game.Platform,
                    Category = game.Genres?.FirstOrDefault(),
                    RemotePath = remotePath,
                    FileName = fileName,
                    InstalledPath = installedPath,
                    InstalledAt = DateTime.UtcNow
                });

                RommLogger.Log($"[RommPlugin] Registered installed: {game.Title} (ID: {gameId})");
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }

        private bool TryDeleteGameFiles(InstalledGameRecord record)
        {
            if (record == null)
            {
                return true;
            }

            var settings = RommPluginStorage.Load();
            var deleted = false;

            RommLogger.Log($"[Uninstall] Trying to delete files for: {record.Title} (GameId={record.RommGameId})");

            if (!string.IsNullOrEmpty(record.InstalledPath))
            {
                RommLogger.Log($"[Uninstall] Trying InstalledPath: {record.InstalledPath}");
                if (Directory.Exists(record.InstalledPath))
                {
                    try { Directory.Delete(record.InstalledPath, true); deleted = true; RommLogger.Log($"[Uninstall] Deleted folder: {record.InstalledPath}"); }
                    catch (Exception ex) { RommLogger.LogError($"[Uninstall] Failed to delete folder {record.InstalledPath}: {ex.Message}"); }
                }
                else if (File.Exists(record.InstalledPath))
                {
                    try { File.Delete(record.InstalledPath); deleted = true; RommLogger.Log($"[Uninstall] Deleted file: {record.InstalledPath}"); }
                    catch (Exception ex) { RommLogger.LogError($"[Uninstall] Failed to delete file {record.InstalledPath}: {ex.Message}"); }
                }
                else
                {
                    RommLogger.Log($"[Uninstall] InstalledPath not found on disk: {record.InstalledPath}");
                }
            }

            if (!deleted && !string.IsNullOrEmpty(record.RemotePath) && !string.IsNullOrEmpty(record.FileName))
            {
                var localFile = Path.Combine(settings.RomsPath, RommConstants.RomsSubfolder, record.RemotePath.Replace("/", "\\"), record.FileName);
                RommLogger.Log($"[Uninstall] Trying constructed path: {localFile}");

                if (Directory.Exists(localFile))
                {
                    try { Directory.Delete(localFile, true); deleted = true; RommLogger.Log($"[Uninstall] Deleted folder: {localFile}"); }
                    catch (Exception ex) { RommLogger.LogError($"[Uninstall] Failed to delete folder {localFile}: {ex.Message}"); }
                }

                if (File.Exists(localFile))
                {
                    try { File.Delete(localFile); deleted = true; RommLogger.Log($"[Uninstall] Deleted file: {localFile}"); }
                    catch (Exception ex) { RommLogger.LogError($"[Uninstall] Failed to delete file {localFile}: {ex.Message}"); }
                }

                if (File.Exists(localFile + ".zip"))
                {
                    try { File.Delete(localFile + ".zip"); deleted = true; RommLogger.Log($"[Uninstall] Deleted file: {localFile}.zip"); }
                    catch (Exception ex) { RommLogger.LogError($"[Uninstall] Failed to delete file {localFile}.zip: {ex.Message}"); }
                }

                if (record.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var withoutZip = localFile.Substring(0, localFile.Length - 4);
                    if (File.Exists(withoutZip))
                    {
                        try { File.Delete(withoutZip); deleted = true; RommLogger.Log($"[Uninstall] Deleted file (without .zip): {withoutZip}"); }
                        catch (Exception ex) { RommLogger.LogError($"[Uninstall] Failed to delete file {withoutZip}: {ex.Message}"); }
                    }
                }
            }

            if (!deleted)
            {
                RommLogger.Log($"[Uninstall] No files found to delete for: {record.Title}");
            }

            return deleted;
        }

        private IGame FindGameByRommId(int rommId)
        {
            var dataManager = PluginHelper.DataManager;
            var rommGames = dataManager.GetAllGames()
                .Where(g => g.Platform != null && g.Platform.StartsWith(RommConstants.PlatformPrefix))
                .ToList();

            foreach (var game in rommGames)
            {
                var value = game.GetAllCustomFields()
                    .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;
                if (int.TryParse(value, out var id) && id == rommId)
                {
                    return game;
                }
            }

            return null;
        }

        private void ClearGameAdditionalApplications(IGame game, string installedPath)
        {
            if (string.IsNullOrEmpty(installedPath)) return;

            var jsonPath = Path.Combine(installedPath, RommConstants.LaunchboxConfigFile);
            if (!File.Exists(jsonPath)) return;

            var config = JsonConvert.DeserializeObject<LaunchBoxFolderGameConfig>(File.ReadAllText(jsonPath));
            if (config == null) return;

            var jsonPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (config.AdditionalApplications != null)
                foreach (var app in config.AdditionalApplications)
                    if (!string.IsNullOrEmpty(app.Path))
                        jsonPaths.Add(ResolveAdditionalAppPath(installedPath, app.Path, false));

            if (config.PreLoaders != null)
                foreach (var loader in config.PreLoaders)
                    if (!string.IsNullOrEmpty(loader.Path))
                        jsonPaths.Add(ResolveAdditionalAppPath(installedPath, loader.Path, loader.FromLaunchBoxRoot ?? false));

            if (config.PosLoaders != null)
                foreach (var loader in config.PosLoaders)
                    if (!string.IsNullOrEmpty(loader.Path))
                        jsonPaths.Add(ResolveAdditionalAppPath(installedPath, loader.Path, loader.FromLaunchBoxRoot ?? false));

            var apps = game.GetAllAdditionalApplications().ToList();
            foreach (var app in apps)
            {
                if (!string.IsNullOrEmpty(app.ApplicationPath) && jsonPaths.Contains(app.ApplicationPath)
                    && app.Name?.StartsWith(RommConstants.PlatformPrefix) == true)
                {
                    game.TryRemoveAdditionalApplication(app);
                }
            }
        }

        private string ResolveAdditionalAppPath(string baseFolder, string path, bool fromLaunchBoxRoot)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            if (fromLaunchBoxRoot) return path;
            return Path.GetFullPath(Path.Combine(baseFolder, path));
        }

        private void DgvGames_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colProgress.Index) return;

            var item = dgvGames.Rows[e.RowIndex].Tag as DownloadItem;
            if (item == null)
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
