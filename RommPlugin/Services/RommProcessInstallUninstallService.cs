using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Services;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using RommPlugin.UI.Helpers;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Processes pending download items (installs and uninstalls) from the RomM server.
    /// Extracts downloaded ROMs, configures LaunchBox games, and handles folder game detection.
    /// </summary>
    public class RommProcessInstallUninstallService : IRommProcessInstallUninstallService
    {
        /// <summary>
        /// How long a trigger waits for a running cross-process pass before
        /// giving up. The wait runs on a pool thread, so it can outlive a long
        /// extraction without freezing the UI. A waiter that gives up is not
        /// lost: the Game Manager retries while the state file still lists
        /// pending installs, and the next pass re-reads that file.
        /// </summary>
        private static readonly TimeSpan InstallLockWait = TimeSpan.FromMinutes(10);

        private bool _lastRunWasCancelled;

        /// <inheritdoc/>
        public bool LastRunWasCancelled
        {
            get { return _lastRunWasCancelled; }
        }

        /// <inheritdoc/>
        public async Task ProcessInstallUninstallEvents(bool showEmptyMessage = true)
        {
            _lastRunWasCancelled = false;

            // LaunchBox spawns a "-recovery" instance after an unclean shutdown.
            // Both instances load the plugin and share the same state files, so
            // mutating work is left to the primary instance only.
            if (LaunchBoxInstance.IsRecoveryInstance())
            {
                RommLogger.Log("[Install] LaunchBox recovery instance: install processing left to the primary instance");
                return;
            }

            // Cross-process single-flight lives here rather than in the caller:
            // the Game Manager closing path, the startup path and a second
            // LaunchBox instance all funnel through this method, and two
            // overlapping runs delete each other's extract folder (which showed
            // up as "installed" rows whose files were gone).
            //
            // Wait instead of skipping: the running pass snapshotted the state
            // file before this trigger's item appeared, so dropping it left the
            // game on "Installing" with the archive intact. The wait runs on a
            // pool thread: this method is called from the Game Manager's UI
            // thread, and blocking it here froze LaunchBox for the whole wait.
            var acquired = await Task.Run(() => InstallProcessLock.TryEnter(InstallLockWait));

            if (!acquired)
                return;

            try
            {
                _lastRunWasCancelled = await ProcessPendingInstallUninstallAsync(showEmptyMessage);
            }
            finally
            {
                InstallProcessLock.Exit();
            }
        }

        private async Task<bool> ProcessPendingInstallUninstallAsync(bool showEmptyMessage)
        {
            var cancelled = false;

            await ProgressRunner.RunAsync(
                LocaleManager.Get("progress.processing"),
                async progress =>
                {
                    var stateFilePath = RommPaths.DownloadStateFile;

                    if (!File.Exists(stateFilePath))
                    {
                        if (showEmptyMessage)
                        {
                            using (var form = new ConfirmForm(LocaleManager.Get("progress.no_pending")))
                            {
                                form.ShowDialog();
                            }
                        }
                        return;
                    }

                    DownloadState state;
                    try
                    {
                        state = JsonConvert.DeserializeObject<DownloadState>(File.ReadAllText(stateFilePath));
                    }
                    catch (Exception ex)
                    {
                        RommLogger.LogError($"[RommPlugin] Failed to read download-state.json: {ex.Message}");
                        return;
                    }

                    var waitingItems = state?.Items?
                        .Where(i => i != null && i.Status == DownloadStatus.WaitingInstall)
                        .ToList();

                    if (waitingItems == null || waitingItems.Count == 0)
                    {
                        if (showEmptyMessage)
                        {
                            using (var form = new ConfirmForm(LocaleManager.Get("progress.no_pending")))
                            {
                                form.ShowDialog();
                            }
                        }
                        return;
                    }

                    var settings = RommPluginStorage.Load();
                    var dataManager = PluginHelper.DataManager;

                    var rommGamesOnly = dataManager.GetAllGames()
                        .Where(g => g.Platform != null && g.Platform.StartsWith(RommConstants.PlatformPrefix))
                        .ToList();

                    var gamesById = new Dictionary<int, IGame>();

                    foreach (var game in rommGamesOnly)
                    {
                        if (RommGameHelpers.TryGetRommId(game, out var id))
                        {
                            gamesById[id] = game;
                        }
                    }

                    var totalItems = waitingItems.Count;
                    var completedItems = 0;
                    // Items whose status this batch may overwrite on save. Any
                    // pending item still WaitingInstall at the end was not
                    // touched (cancelled batch) and must not be rewritten.
                    var pendingIds = new HashSet<int>(waitingItems.Select(i => i.GameId));

                    foreach (var item in waitingItems)
                    {
                        // The user may close the progress window between items:
                        // stop cleanly instead of holding the close vetoed for
                        // the whole remaining batch, and fall through to the
                        // state save below so the items already processed are
                        // persisted. Throwing here would skip that save and
                        // replay them as WaitingInstall with their archive
                        // already consumed (a permanent failure). Unprocessed
                        // items stay WaitingInstall and are picked up next run.
                        if (progress.CancellationToken.IsCancellationRequested)
                        {
                            cancelled = true;
                            break;
                        }

                        progress.SetStatus($"Processing: {completedItems} of {totalItems}");

                        string attemptZipPath = null;
                        string attemptExtractDir = null;
                        bool attemptExtractExisted = false;
                        bool attemptPublishedExtractDir = false;
                        IGame game = null;

                        try
                        {
                            if (!gamesById.TryGetValue(item.GameId, out game))
                            {
                                SetStatus(item, DownloadStatus.Failed);
                                item.Error = "Game not found in LaunchBox";
                                completedItems++;
                                continue;
                            }

                            var fields = game.GetAllCustomFields().GroupBy(f => f.Name).ToDictionary(g => g.Key, g => g.Last().Value);

                            fields.TryGetValue(GameCustomFields.RemotePath, out var remotePath);
                            fields.TryGetValue(GameCustomFields.FileName, out var fileName);

                            if (string.IsNullOrWhiteSpace(remotePath) || string.IsNullOrWhiteSpace(fileName))
                            {
                                RommLogger.LogError($"[RommPlugin] Game {game.Title} (ID: {item.GameId}) is missing RemotePath or FileName custom fields, skipping");
                                SetStatus(item, DownloadStatus.Failed);
                                item.Error = "Missing custom fields";
                                completedItems++;
                                continue;
                            }

                            var isFolderGame = !Path.HasExtension(fileName);

                            var localFile = Path.Combine(
                                settings.RomsPath,
                                RommConstants.RomsSubfolder,
                                remotePath.Replace("/", "\\"),
                                fileName
                            );

                            var zipPath = localFile;

                            if (!File.Exists(zipPath))
                            {
                                var withZip = zipPath + ".zip";
                                if (File.Exists(withZip))
                                    zipPath = withZip;
                                else if (zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                {
                                    var withoutZip = zipPath.Substring(0, zipPath.Length - 4);
                                    if (File.Exists(withoutZip))
                                        zipPath = withoutZip;
                                }
                            }

                            if (!isFolderGame && File.Exists(zipPath) && Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var archive = ZipFile.OpenRead(zipPath))
                                {
                                    var entries = archive.Entries.Where(e => !string.IsNullOrWhiteSpace(e.Name)).ToList();
                                    var hasSubdirs = entries.Any(e => e.FullName.Contains("/"));
                                    if (hasSubdirs && entries.Count > 1)
                                    {
                                        isFolderGame = true;
                                        var newFileName = Path.GetFileNameWithoutExtension(fileName);
                                        var field = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.FileName);
                                        if (field != null) field.Value = newFileName;
                                        else
                                        {
                                            var newField = game.AddNewCustomField();
                                            newField.Name = GameCustomFields.FileName;
                                            newField.Value = newFileName;
                                        }
                                        fileName = newFileName;
                                    }
                                }
                            }

                            if (!File.Exists(zipPath) && !isFolderGame)
                            {
                                RommLogger.LogError($"[Install] Game {item.GameId} '{item.GameName}': ROM file not found at '{zipPath}', marking as failed");
                                SetStatus(item, DownloadStatus.Failed);
                                item.Error = "ROM file not found";
                                completedItems++;
                                continue;
                            }

                            var extractDir = Path.Combine(
                                Path.GetDirectoryName(zipPath),
                                Path.GetFileNameWithoutExtension(zipPath)
                            );

                            attemptZipPath = zipPath;
                            attemptExtractDir = extractDir;
                            attemptExtractExisted = Directory.Exists(extractDir);

                            if (isFolderGame)
                            {
                                // The archive must be a real file: when only the folder
                                // exists (incomplete download), opening it as a zip
                                // throws access-denied noise. Fail fast instead.
                                if (!File.Exists(zipPath))
                                {
                                    // Unless the folder is already a finished install:
                                    // an earlier pass may have extracted it and never
                                    // got its status committed (a refused trigger, or a
                                    // state file clobbered before the write landed).
                                    // Failing here told the user to delete the very
                                    // folder that is installed, which is why the game
                                    // stayed stuck on "Installing" forever.
                                    if (InstallValidation.IsFolderInstallValid(extractDir))
                                    {
                                        RommLogger.Log($"[Install] Game {item.GameId} '{item.GameName}': archive already consumed, folder is complete at '{extractDir}'; completing the install");

                                        var completeJsonPath = Path.Combine(extractDir, RommConstants.LaunchboxConfigFile);
                                        if (File.Exists(completeJsonPath))
                                        {
                                            ConfigureLaunchBoxGame(game, extractDir, completeJsonPath);
                                        }

                                        localFile = extractDir;
                                        game.Installed = true;
                                        SetStatus(item, DownloadStatus.Installed);
                                        item.CompletedAt = DateTime.UtcNow;
                                        completedItems++;
                                        continue;
                                    }

                                    RommLogger.LogError($"[Install] Game {item.GameId} '{item.GameName}': archive not found at '{zipPath}' (incomplete download?). Delete the game folder, requeue the download and retry.");
                                    SetStatus(item, DownloadStatus.Failed);
                                    item.Error = "Archive missing - delete folder and requeue download";
                                    completedItems++;
                                    continue;
                                }

                                UnzipAndDelete(zipPath, extractDir);
                                // Only set once the staging→destination move
                                // succeeded: from here on the folder is ours.
                                attemptPublishedExtractDir = true;

                                var jsonPath = Path.Combine(extractDir, RommConstants.LaunchboxConfigFile);

                                if (File.Exists(jsonPath))
                                {
                                    ConfigureLaunchBoxGame(game, extractDir, jsonPath);
                                }

                                localFile = extractDir;
                            }
                            else
                            {
                                UnzipAndFlatten(zipPath);

                                if (!File.Exists(localFile) && Directory.Exists(extractDir))
                                {
                                    localFile = extractDir;

                                    var jsonPath = Path.Combine(extractDir, RommConstants.LaunchboxConfigFile);
                                    if (File.Exists(jsonPath))
                                    {
                                        ConfigureLaunchBoxGame(game, extractDir, jsonPath);
                                    }
                                }
                                else if (!File.Exists(localFile) && !Directory.Exists(localFile))
                                {
                                    var zipVariant = localFile + ".zip";
                                    if (File.Exists(zipVariant))
                                    {
                                        localFile = zipVariant;
                                    }
                                    else
                                    {
                                        var parentDir = Path.GetDirectoryName(zipPath);
                                        if (Directory.Exists(parentDir))
                                        {
                                            var extracted = Directory.GetFiles(parentDir)
                                                .Where(f => f != zipPath && !f.EndsWith(".json"))
                                                .FirstOrDefault();
                                            if (extracted != null)
                                            {
                                                localFile = extracted;
                                            }
                                        }
                                    }
                                }

                                game.ApplicationPath = File.Exists(localFile) ? localFile : (Directory.Exists(localFile) ? localFile : null);
                            }

                            game.Installed = isFolderGame ? Directory.Exists(localFile) : File.Exists(localFile);

                            SetStatus(item, DownloadStatus.Installed);
                            item.CompletedAt = DateTime.UtcNow;
                            completedItems++;
                        }
                        catch (Exception ex)
                        {
                            var context = $"[Install] Game {item.GameId} '{item.GameName}' zip='{attemptZipPath ?? "(unknown)"}'";
                            // Deliberately NOT clearing game.Installed here. A
                            // failed attempt says nothing about what is already
                            // on disk: flagging it as uninstalled is how a valid
                            // install used to vanish. The installed-games record
                            // (and its repair pass) owns that flag.
                            if (ex is InvalidDataException)
                            {
                                RommLogger.LogError($"{context} archive is unreadable (corrupt/truncated): {ex.Message}");
                                RommOrphanCleanupService.CleanInstallAttempt(
                                    settings.RomsPath,
                                    item.GameId,
                                    item.GameName,
                                    null,
                                    attemptExtractDir,
                                    InstallFailureCleanup.ShouldDeleteExtractDir(attemptExtractExisted, attemptPublishedExtractDir),
                                    item.PartFilePath,
                                    attemptZipPath,
                                    true);
                                SetStatus(item, DownloadStatus.Failed);
                                item.Error = RommConstants.CorruptArchiveMarker;
                            }
                            else
                            {
                                RommLogger.LogError($"{context} failed: {ex}");
                                RommOrphanCleanupService.CleanInstallAttempt(
                                    settings.RomsPath,
                                    item.GameId,
                                    item.GameName,
                                    null,
                                    attemptExtractDir,
                                    InstallFailureCleanup.ShouldDeleteExtractDir(attemptExtractExisted, attemptPublishedExtractDir),
                                    // Keep the .part so the retry resumes instead of
                                    // restarting large downloads from zero. Only corrupt
                                    // archives (above) drop it for a clean redownload.
                                    null,
                                    attemptZipPath,
                                    false);
                                SetStatus(item, DownloadStatus.Failed);
                                item.Error = ex.Message;
                            }
                        }
                    }

                    // The user may cancel while the last item is finishing; the
                    // loop-top check misses it and the caller would reopen the
                    // progress window it just closed.
                    cancelled = cancelled || progress.CancellationToken.IsCancellationRequested;

                    // Save only what this batch settled, merged into a fresh
                    // snapshot: the download queue may have added or progressed
                    // items while the extraction ran, and writing this batch's
                    // stale snapshot would roll them back (or erase a verdict
                    // another instance already committed).
                    var touched = new Dictionary<int, DownloadItem>();
                    foreach (var item in state.Items ?? new List<DownloadItem>())
                    {
                        if (item == null || !pendingIds.Contains(item.GameId)) continue;
                        if (item.Status == DownloadStatus.WaitingInstall) continue;

                        touched[item.GameId] = item;
                    }

                    if (touched.Count > 0)
                    {
                        try
                        {
                            var onDisk = ReadStateItems(stateFilePath);
                            if (onDisk == null)
                            {
                                // Unreadable (not "empty": an empty list means the
                                // user cleared the queue, which must be respected).
                                // Fall back to this batch's own snapshot rather
                                // than writing an empty queue that loses verdicts.
                                onDisk = state.Items ?? new List<DownloadItem>();
                            }

                            var merged = DownloadStateMerger.MergeForInstallSave(touched, onDisk);
                            var mergedState = new DownloadState
                            {
                                Items = merged,
                                LastUpdated = DateTime.UtcNow
                            };

                            var json = JsonConvert.SerializeObject(mergedState, Formatting.Indented);
                            SafeFileWriter.WriteAllText(stateFilePath, json);
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"[RommPlugin] Failed to save download-state.json: {ex.Message}");
                        }
                    }

                    dataManager.Save();

                    if (cancelled)
                    {
                        RommLogger.Log("[Install] Cancelled by the user; kept the status of the items processed so far");
                    }
                    else
                    {
                        RommLogger.Log("Pending installs processed successfully");

                        if (showEmptyMessage)
                        {
                            using (var form = new ConfirmForm(LocaleManager.Get("progress.finished")))
                            {
                                form.ShowDialog();
                            }
                        }
                    }
                }
            );

            return cancelled;
        }

        /// <summary>
        /// Changes the item status and stamps it: the state file is shared, so
        /// the stamp lets the other process tell this verdict apart from an
        /// older one instead of reapplying a stale status.
        /// </summary>
        private static void SetStatus(DownloadItem item, DownloadStatus status)
        {
            item.Status = status;
            item.StatusUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Fresh read of the shared state file. Returns <c>null</c> when the
        /// file exists but cannot be parsed, so callers can fall back instead of
        /// writing an empty queue over it. A valid file with no items is an
        /// empty list, not a failure: the user cleared the queue.
        /// </summary>
        private static List<DownloadItem> ReadStateItems(string stateFilePath)
        {
            try
            {
                if (!File.Exists(stateFilePath)) return new List<DownloadItem>();

                var json = File.ReadAllText(stateFilePath);
                var state = JsonConvert.DeserializeObject<DownloadState>(json);
                if (state == null || state.Items == null) return null;

                return state.Items;
            }
            catch
            {
                return null;
            }
        }

        private void ConfigureLaunchBoxGame(IGame game, string baseFolder, string jsonPath)
        {
            var config = JsonConvert.DeserializeObject<LaunchBoxFolderGameConfig>(File.ReadAllText(jsonPath));

            if (config == null)
            {
                RommLogger.LogError($"[RommPlugin] Failed to deserialize LaunchBox config from {jsonPath}");
                return;
            }

            RommGameHelpers.ClearGameAdditionalApplications(game, baseFolder);

            if (!string.IsNullOrWhiteSpace(config.DefaultFileName))
            {
                game.ApplicationPath = Path.GetFullPath(Path.Combine(baseFolder, config.DefaultFileName));
            }
            else
            {
                // Empty DefaultFileName: the folder itself is the game reference
                // (e.g. PS3 disc folders booted in place by an emulator wrapper).
                game.ApplicationPath = Path.GetFullPath(baseFolder);
            }

            var initialApps = game.GetAllAdditionalApplications().ToList();

            var existingPaths = new HashSet<string>(
                initialApps
                    .Where(a => !string.IsNullOrEmpty(a.ApplicationPath))
                    .Select(a => a.ApplicationPath),
                StringComparer.OrdinalIgnoreCase);

            if (config.AdditionalApplications != null)
            {
                var route = DlcEntryRouting.Decide(config.DlcUseEmulator, game.EmulatorId);
                var initialByPath = initialApps
                    .Where(a => !string.IsNullOrEmpty(a.ApplicationPath))
                    .GroupBy(a => a.ApplicationPath, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                foreach (var app in config.AdditionalApplications)
                {
                    var resolvedPath = RommGameHelpers.ResolvePath(baseFolder, app.Path, false);
                    if (existingPaths.Contains(resolvedPath))
                    {
                        // Align pre-existing entries THAT THIS PLUGIN created
                        // (name carries the platform prefix) to the routed form.
                        // Entries added by the user or another tool are never
                        // touched, nor are pre/post loaders sharing a path.
                        if (route.UseEmulator
                            && initialByPath.TryGetValue(resolvedPath, out var current)
                            && IsPluginOwnedEntry(current.Name)
                            && current.AutoRunBefore != true
                            && current.AutoRunAfter != true)
                        {
                            current.UseEmulator = true;
                            current.EmulatorId = route.EmulatorId;
                            current.WaitForExit = true;
                        }
                        continue;
                    }

                    var add = game.AddNewAdditionalApplication();
                    add.Name = $"{RommConstants.PlatformPrefix}{app.Name}";
                    add.ApplicationPath = resolvedPath;
                    add.CommandLine = app.CommandLine;
                    if (route.UseEmulator)
                    {
                        add.UseEmulator = true;
                        add.EmulatorId = route.EmulatorId;
                        add.WaitForExit = true;
                    }
                    existingPaths.Add(resolvedPath);
                }
            }

            if (config.PreLoaders != null)
            {
                foreach (var loader in config.PreLoaders)
                {
                    var resolvedPath = RommGameHelpers.ResolvePath(baseFolder, loader.Path, loader.FromLaunchBoxRoot ?? false);
                    if (existingPaths.Contains(resolvedPath)) continue;

                    var add = game.AddNewAdditionalApplication();
                    add.Name = $"{RommConstants.PlatformPrefix}{loader.Name}";
                    add.ApplicationPath = resolvedPath;
                    add.CommandLine = loader.CommandLine;
                    add.AutoRunBefore = true;
                    add.WaitForExit = loader.WaitForExit ?? false;
                    existingPaths.Add(resolvedPath);
                }
            }

            if (config.PosLoaders != null)
            {
                foreach (var loader in config.PosLoaders)
                {
                    var resolvedPath = RommGameHelpers.ResolvePath(baseFolder, loader.Path, loader.FromLaunchBoxRoot ?? false);
                    if (existingPaths.Contains(resolvedPath)) continue;

                    var add = game.AddNewAdditionalApplication();
                    add.Name = $"{RommConstants.PlatformPrefix}{loader.Name}";
                    add.ApplicationPath = resolvedPath;
                    add.CommandLine = loader.CommandLine;
                    add.AutoRunAfter = true;
                    existingPaths.Add(resolvedPath);
                }
            }

            if (config.HasDLC == true)
            {
                var dlcFolder = Path.Combine(baseFolder, "_DLCs");

                if (Directory.Exists(dlcFolder))
                {
                    var files = Directory.GetFiles(dlcFolder);
                    var route = DlcEntryRouting.Decide(config.DlcUseEmulator, game.EmulatorId);

                    int index = 1;
                    foreach (var file in files)
                    {
                        if (existingPaths.Contains(file)) continue;

                        var add = game.AddNewAdditionalApplication();
                        add.Name = $"{RommConstants.PlatformPrefix}DLC {index}";
                        add.ApplicationPath = file;
                        if (route.UseEmulator)
                        {
                            add.UseEmulator = true;
                            add.EmulatorId = route.EmulatorId;
                            add.WaitForExit = true;
                        }
                        existingPaths.Add(file);
                        index++;
                    }

                    // Align pre-existing _DLCs entries created by this plugin
                    // (name carries the platform prefix) to the routed form.
                    if (route.UseEmulator)
                    {
                        string dlcRoot = Path.GetFullPath(dlcFolder) + Path.DirectorySeparatorChar;
                        foreach (var current in initialApps)
                        {
                            if (string.IsNullOrEmpty(current.ApplicationPath)) continue;
                            if (!current.ApplicationPath.StartsWith(dlcRoot, StringComparison.OrdinalIgnoreCase)) continue;
                            if (!IsPluginOwnedEntry(current.Name)) continue;
                            if (current.AutoRunBefore == true || current.AutoRunAfter == true) continue;
                            current.UseEmulator = true;
                            current.EmulatorId = route.EmulatorId;
                            current.WaitForExit = true;
                        }
                    }
                }
            }
        }



        /// <summary>
        /// True when an additional application was created by this plugin, which
        /// always prefixes its entries. Keeps DLC routing from mutating entries
        /// the user (or another tool) added to the same game.
        /// </summary>
        private static bool IsPluginOwnedEntry(string name)
        {
            return !string.IsNullOrEmpty(name)
                && name.StartsWith(RommConstants.PlatformPrefix, StringComparison.Ordinal);
        }

        private void UnzipAndDelete(string zipPath, string extractDir)
        {
            var rootFolder = Path.GetFileNameWithoutExtension(zipPath);

            // Fail fast when the destination is not writable instead of dying
            // gigabytes into the extraction.
            var parentDir = Path.GetDirectoryName(Path.GetFullPath(extractDir));
            if (!string.IsNullOrEmpty(parentDir))
                RommArchiveHelper.EnsureWritableDirectory(parentDir);

            // Extract into staging first so a mid-extraction failure never leaves
            // a partial folder behind; publish with an atomic move on success.
            var stagingDir = extractDir + "._tmp_" + Guid.NewGuid().ToString("N");

            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entryList = archive.Entries.ToList();

                    foreach (var entry in entryList)
                    {
                        if (string.IsNullOrWhiteSpace(entry.Name))
                        {
                            continue;
                        }

                        var parts = entry.FullName
                            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                            .SkipWhile(p => p != rootFolder)
                            .Skip(1)
                            .ToArray();

                        if (parts.Length == 0)
                        {
                            parts = entry.FullName
                                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                                .ToArray();
                        }

                        var relativePath = Path.Combine(parts);
                        var destinationPath = Path.Combine(stagingDir, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        // Transient locks (antivirus/indexer grabbing new files)
                        // must slow extraction down, not kill a multi-GB run.
                        var entryAttempts = 0;
                        while (true)
                        {
                            try
                            {
                                entry.ExtractToFile(destinationPath, true);
                                break;
                            }
                            catch (IOException) when (entryAttempts < 2)
                            {
                                entryAttempts++;
                                Thread.Sleep(500);
                            }
                            catch (UnauthorizedAccessException) when (entryAttempts < 2)
                            {
                                entryAttempts++;
                                Thread.Sleep(500);
                            }
                        }
                    }
                }

                if (Directory.Exists(extractDir))
                {
                    // Replace the leftover folder from a previous (failed)
                    // attempt. Safe now that install runs are single-flight
                    // (InstallProcessLock): no other run can own this folder.
                    if (!RommArchiveHelper.DeleteDirectoryRobust(extractDir))
                        throw new IOException($"Could not clear existing folder '{extractDir}' (locked?). Close other programs and retry.");
                }

                Directory.Move(stagingDir, extractDir);

                if (!RommArchiveHelper.DeleteFileWithRetry(zipPath))
                {
                    // Extraction already succeeded: a locked zip must not fail
                    // the whole install. It can be removed manually later.
                    RommLogger.LogError($"[Install] Extracted '{extractDir}' but could not delete source archive '{zipPath}' (locked?). Delete it manually when possible.");
                }
            }
            catch
            {
                RommArchiveHelper.DeleteDirectoryRobust(stagingDir);
                throw;
            }
        }

        private void UnzipAndFlatten(string zipPath)
        {
            if (!File.Exists(zipPath) || !Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var parentDir = Path.GetDirectoryName(Path.GetFullPath(zipPath));
            if (!string.IsNullOrEmpty(parentDir))
                RommArchiveHelper.EnsureWritableDirectory(parentDir);

            var tempExtract = Path.Combine(
                Path.GetDirectoryName(zipPath),
                $"_temp_{Guid.NewGuid():N}"
            );

            if (Directory.Exists(tempExtract))
            {
                Directory.Delete(tempExtract, true);
            }

            Directory.CreateDirectory(tempExtract);

            // Every file copied out of temp is tracked so a failure between the
            // copy and the zip deletion leaves no orphans behind.
            var copiedFiles = new List<string>();

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempExtract);

                var allFiles = Directory.GetFiles(tempExtract, "*", SearchOption.AllDirectories);

                if (allFiles.Length > 1)
                {
                    var gameName = Path.GetFileNameWithoutExtension(zipPath);

                    foreach (var file in allFiles)
                    {
                        var rel = file.Substring(tempExtract.Length + 1);
                        var idx = rel.IndexOf(gameName, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            rel = rel.Substring(idx + gameName.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                        }
                        else if (rel.StartsWith("roms" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                                 rel.StartsWith("roms/", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = rel.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                                rel = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(2));
                        }

                        var dest = Path.Combine(Path.GetDirectoryName(zipPath), rel);
                        RommArchiveHelper.CopyFileRobust(file, dest);
                        copiedFiles.Add(dest);
                    }

                    if (!RommArchiveHelper.DeleteFileWithRetry(zipPath))
                    {
                        throw new IOException($"Could not delete source archive after extraction: {zipPath}");
                    }
                }
                else if (allFiles.Length == 1)
                {
                    var innerFile = allFiles[0];
                    var innerExt = Path.GetExtension(innerFile).ToLowerInvariant();

                    if (innerExt == ".zip")
                    {
                        RommArchiveHelper.CopyFileRobust(innerFile, zipPath);
                    }
                    else
                    {
                        var targetPath = Path.Combine(
                            Path.GetDirectoryName(zipPath),
                            Path.GetFileName(innerFile)
                        );
                        RommArchiveHelper.CopyFileRobust(innerFile, targetPath);
                        copiedFiles.Add(targetPath);
                        if (!RommArchiveHelper.DeleteFileWithRetry(zipPath))
                        {
                            throw new IOException($"Could not delete source archive after extraction: {zipPath}");
                        }
                    }
                }
            }
            catch
            {
                foreach (var copied in copiedFiles)
                {
                    RommArchiveHelper.DeleteFileWithRetry(copied, 1, 0);
                }
                throw;
            }
            finally
            {
                RommArchiveHelper.DeleteDirectoryRobust(tempExtract);
            }
        }
    }
}
