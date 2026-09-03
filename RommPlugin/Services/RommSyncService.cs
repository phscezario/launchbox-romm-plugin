using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using RommPlugin.UI.Forms;
using RommPlugin.UI.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Orchestrates bidirectional synchronization of platforms, games, metadata, screenshots, and play statistics between LaunchBox and a RomM server.
    /// </summary>
    public class RommSyncService : IRommSyncService
    {
        private IRommApiClient _api;
        private readonly IRommMetadataMapper _metadataMapper;
        private readonly IRommScreenshotSync _screenshotSync;
        private readonly IRommStatsService _statsService;
        private readonly IRommHierarchyCli _hierarchyCli;
        private readonly IRommBackupService _backupService;
        private static int _isRunning = 0;

        /// <inheritdoc/>
        public IRommApiClient Api => _api;

        /// <summary>
        /// Initializes a new instance of the <see cref="RommSyncService"/> class with default dependencies.
        /// </summary>
        public RommSyncService()
        {
            _metadataMapper = new RommMetadataMapper();
            _hierarchyCli = new RommHierarchyCli();
            _backupService = new RommBackupService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RommSyncService"/> class with the specified dependencies.
        /// </summary>
        /// <param name="api">The RomM API client for server communication.</param>
        /// <param name="metadataMapper">The metadata mapper for applying server data to local games.</param>
        /// <param name="screenshotSync">The screenshot synchronization service.</param>
        /// <param name="statsService">The play statistics synchronization service.</param>
        /// <param name="hierarchyCli">The hierarchy CLI launcher for rebuilding playlists.</param>
        /// <param name="backupService">The XML backup service.</param>
        public RommSyncService(
            IRommApiClient api,
            IRommMetadataMapper metadataMapper,
            IRommScreenshotSync screenshotSync,
            IRommStatsService statsService,
            IRommHierarchyCli hierarchyCli,
            IRommBackupService backupService)
        {
            _api = api;
            _metadataMapper = metadataMapper;
            _screenshotSync = screenshotSync;
            _statsService = statsService;
            _hierarchyCli = hierarchyCli;
            _backupService = backupService;
        }

        /// <inheritdoc/>
        public void SetApi(IRommApiClient api)
        {
            _api = api;
        }

        /// <inheritdoc/>
        public async Task SyncAsync(bool headless = false)
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                if (!headless)
                {
                    using (var form = new ConfirmForm(LocaleManager.Get("sync.already_running")))
                    {
                        form.ShowDialog();
                    }
                }
                return;
            }

            try
            {
                await ProgressRunner.RunAsync(
                    "Starting sync from RomM...",
                    async progress =>
                    {
                    var settings = RommPluginStorage.Load();

                    _api.ApplyAuthentication(settings);

                    var dataManager = PluginHelper.DataManager;

                    var platforms = dataManager.GetAllPlatforms()
                        .Where(p => p.Name != null && p.Name.StartsWith(RommConstants.PlatformPrefix))
                        .ToList();

                    var rommGamesOnly = dataManager.GetAllGames()
                        .Where(g => g.Platform != null && g.Platform.StartsWith(RommConstants.PlatformPrefix))
                        .ToList();

                    var platformCategories = dataManager.GetAllPlatformCategories()
                        .Where(c => c.Name != null && c.Name.StartsWith(RommConstants.PlatformPrefix))
                        .ToList();

                    var allRommPlatforms = await _api.GetPlatformsAsync();

                    if (allRommPlatforms == null || allRommPlatforms.Count == 0)
                    {
                        RommLogger.Log("No platforms found on RomM server. Sync skipped.");
                        return;
                    }

                    var localPlatformNames = new HashSet<string>(
                        platforms.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                    List<RommPlatform> rommPlatforms;

                    if (headless)
                    {
                        if (settings.LastSelectedPlatformIds != null && settings.LastSelectedPlatformIds.Count > 0)
                        {
                            var selectedIds = new HashSet<int>(settings.LastSelectedPlatformIds);
                            rommPlatforms = allRommPlatforms
                                .Where(p => selectedIds.Contains(p.Id))
                                .ToList();
                            RommLogger.Log($"Auto-sync: {rommPlatforms.Count} pre-selected platforms to sync (from {allRommPlatforms.Count} on server)");
                        }
                        else
                        {
                            rommPlatforms = allRommPlatforms
                                .Where(p =>
                                {
                                    var name = $"RomM | {(string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName)}";
                                    return localPlatformNames.Contains(name);
                                })
                                .ToList();
                            RommLogger.Log($"Auto-sync: {rommPlatforms.Count} already-synced platforms to sync (from {allRommPlatforms.Count} on server)");
                        }

                        if (rommPlatforms.Count == 0)
                        {
                            RommLogger.Log("No platforms to sync. Sync skipped.");
                            return;
                        }
                    }
                    else
                    {
                        rommPlatforms = allRommPlatforms;
                    }

                    RommLogger.Log($"Sync started: {rommPlatforms.Count} platforms to sync, {rommGamesOnly.Count} local RomM games");

                    var platformCategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var rp in allRommPlatforms)
                    {
                        var pname = $"RomM | {(string.IsNullOrEmpty(rp.CustomName) ? rp.Name : rp.CustomName)}";
                        platformCategoryMap[pname] = $"RomM | {RommGameHelpers.ParseCategory(rp.Category)}";
                    }

                    var localGamesById = new Dictionary<int, IGame>();

                    foreach (var game in rommGamesOnly)
                    {
                        if (RommGameHelpers.TryGetRommId(game, out var id))
                        {
                            localGamesById[id] = game;
                        }
                    }

                    var selectedPlatformIds = new HashSet<int>();
                    var sameSelection = false;

                    if (headless)
                    {
                        selectedPlatformIds = rommPlatforms.Select(p => p.Id).ToHashSet();
                    }
                    else
                    {
                        var list = rommPlatforms.Select(p =>
                        {
                            var displayName = string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName;
                            var fullName = $"RomM | {displayName}";
                            return new PlatformSelection
                            {
                                Id = p.Id,
                                Name = displayName,
                                Selected = localPlatformNames.Contains(fullName)
                            };
                        }).ToList();

                        var initialSelection = new HashSet<int>(
                            list.Where(p => p.Selected).Select(p => p.Id));

                        using (var form = new RommPlatformSelectorForm(list))
                        {
                            if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                            {
                                return;
                            }

                            selectedPlatformIds = form.Platforms
                                .Where(p => p.Selected)
                                .Select(p => p.Id)
                                .ToHashSet();

                            sameSelection = selectedPlatformIds.SetEquals(initialSelection);

                            settings.LastSelectedPlatformIds = selectedPlatformIds.OrderBy(id => id).ToList();
                            RommPluginStorage.Save(settings);
                        }
                    }

                    var hasChanges = false;

                    var platformCompleted = 0;
                    var platformTotal = selectedPlatformIds.Count;

                    var newPlatforms = allRommPlatforms
                        .Where(p =>
                        {
                            var displayName = string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName;
                            var fullName = $"RomM | {displayName}";
                            return !localPlatformNames.Contains(fullName);
                        })
                        .Select(p => $"RomM | {(string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName)}")
                        .ToList();
                    var removedGames = new List<IGame>();

                    var rootRommCategory = platformCategories.FirstOrDefault(p => p.Name == RommConstants.RootCategoryName);
                    if (rootRommCategory == null)
                    {
                        try
                        {
                            rootRommCategory = dataManager.AddNewPlatformCategory(RommConstants.RootCategoryName);
                            platformCategories.Add(rootRommCategory);
                            hasChanges = true;
                            RommLogger.Log("Created root 'RomM' platform category in Platforms.xml");
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to create root RomM category: {ex.Message}");
                        }
                    }

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        progress.CancellationToken.ThrowIfCancellationRequested();

                        var name = !string.IsNullOrWhiteSpace(rommPlatform.CustomName)
                            ? rommPlatform.CustomName
                            : rommPlatform.Name;

                        var platformName = $"RomM | {name}";

                        if (!selectedPlatformIds.Contains(rommPlatform.Id))
                        {
                            continue;
                        }

                        var platform = platforms
                            .FirstOrDefault(p => string.Equals(p.Name, platformName, StringComparison.OrdinalIgnoreCase));

                        if (platform == null)
                        {
                            var parsedCategory = RommGameHelpers.ParseCategory(rommPlatform.Category);
                            var rommCategoryName = $"RomM | {parsedCategory}";

                            var rommCategory = platformCategories
                                .FirstOrDefault(p => p.Name == rommCategoryName);

                            if (rommCategory == null)
                            {
                                rommCategory = dataManager.AddNewPlatformCategory(rommCategoryName);
                                platformCategories.Add(rommCategory);
                                hasChanges = true;
                            }

                            platform = dataManager.AddNewPlatform(platformName);
                            platform.Category = rommCategoryName;
                            platforms.Add(platform);
                            hasChanges = true;
                        }

                        platformCompleted++;

                        var rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);

                        if (rommGames == null)
                        {
                            continue;
                        }

                        RommLogger.Log($"Platform '{platform.Name}': {rommGames.Count} games to process");

                        progress.SetTitle($"RomM: Syncing {platform.Name}");

                        var completedGames = 0;
                        var totalGames = rommGames.Count;
                        var saveCounter = 0;

                        var serverGameIds = new HashSet<int>();

                        var platformProgress = false;

                        foreach (var rommGame in rommGames)
                        {
                            progress.CancellationToken.ThrowIfCancellationRequested();

                            if (!platformProgress)
                            {
                                progress.SetIndeterminate(false);
                                platformProgress = true;
                            }

                            progress.SetStatus($"Platform {platformCompleted}/{platformTotal} | Games {completedGames}/{totalGames}");
                            progress.SetProgress((completedGames * 100) / Math.Max(totalGames, 1));

                            serverGameIds.Add(rommGame.Id);

                            var remoteHash = RommMetadataComparer.ComputeRemoteMetadataHash(rommGame);

                            if (localGamesById.TryGetValue(rommGame.Id, out var existingGame))
                            {
                                var localHash = RommMetadataComparer.ComputeLocalMetadataHash(existingGame);
                                var savedLocalHash = RommGameHelpers.GetCustomField(existingGame, GameCustomFields.LocalMetadataHash);
                                var savedRemoteHash = RommGameHelpers.GetCustomField(existingGame, GameCustomFields.RemoteMetadataHash);

                                RommLogger.Log($"[HASH-COMPARE] Game {rommGame.Id} '{rommGame.Name}': localHash={localHash} savedLocalHash={savedLocalHash ?? "null"} savedRemoteHash={savedRemoteHash ?? "null"} remoteHash={remoteHash} localMatch={localHash == savedLocalHash} remoteMatch={remoteHash == savedRemoteHash}");

                                if (!settings.ForceFullResync && !settings.ForcePushToServer && localHash == savedLocalHash && remoteHash == savedRemoteHash)
                                {
                                    completedGames++;
                                    continue;
                                }

                                var remoteFull = await _api.GetGameByIdAsync(rommGame.Id);
                                if (remoteFull == null)
                                {
                                    completedGames++;
                                    continue;
                                }

                                if (settings.ForcePushToServer && settings.IsAdmin)
                                {
                                    try
                                    {
                                        RommLogger.Log($"[FORCE-PUSH] Game {rommGame.Id}: ForcePushToServer active, pushing local to server");
                                        await PushGameMetadataAsync(existingGame, remoteFull, settings);
                                        hasChanges = true;

                                        RommGameHelpers.SaveSyncHashes(existingGame, remoteHash);
                                    }
                                    catch (Exception ex)
                                    {
                                        RommLogger.LogError($"Failed to force push metadata for game {rommGame.Id}: {ex.Message}");
                                    }
                                }
                                else if (settings.KeepLocalData)
                                {
                                    if (settings.IsAdmin)
                                    {
                                        try
                                        {
                                            await PushGameMetadataAsync(existingGame, remoteFull, settings);
                                            hasChanges = true;

                                            RommGameHelpers.SaveSyncHashes(existingGame, remoteHash);
                                        }
                                        catch (Exception ex)
                                        {
                                            RommLogger.LogError($"Failed to push metadata for game {rommGame.Id}: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        hasChanges = true;

                                        RommGameHelpers.SaveSyncHashes(existingGame, remoteHash);
                                    }
                                }
                                else
                                {
                                    _metadataMapper.ApplyServerMetadata(existingGame, remoteFull, settings);

                                    if (!_screenshotSync.HasAnyBoxFrontImage(existingGame))
                                    {
                                        await _screenshotSync.DownloadAndSetCoverArt(existingGame, remoteFull);
                                    }

                                    await _screenshotSync.SyncScreenshotsBidirectional(existingGame, remoteFull, settings);
                                    hasChanges = true;

                                    RommGameHelpers.SaveSyncHashes(existingGame, remoteHash);
                                }
                            }
                            else
                            {
                                var normalizedTitle = RommGameHelpers.NormalizeGameTitle(rommGame.Name);
                                RommLogger.Log($"Game {rommGame.Id} created: {normalizedTitle}");
                                var game = dataManager.AddNewGame(normalizedTitle);

                                game.Platform = platform.Name;

                                var isFolderGame = rommGame.HasMultipleFiles;

                                RommGameHelpers.SetCustomField(game, GameCustomFields.GameId, rommGame.Id.ToString());
                                RommGameHelpers.SetCustomField(game, GameCustomFields.PlatformId, rommPlatform.Id.ToString());
                                RommGameHelpers.SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
                                RommGameHelpers.SetCustomField(game, GameCustomFields.FileName, rommGame.FsName);
                                RommGameHelpers.SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());

                                game.Installed = game.Installed != null ? game.Installed : false;

                                localGamesById[rommGame.Id] = game;

                                var remoteFull = await _api.GetGameByIdAsync(rommGame.Id);
                                if (remoteFull != null)
                                {
                                    _metadataMapper.ApplyServerMetadata(game, remoteFull, settings);

                                    if (!_screenshotSync.HasAnyBoxFrontImage(game))
                                    {
                                        await _screenshotSync.DownloadAndSetCoverArt(game, remoteFull);
                                    }

                                    await _screenshotSync.SyncScreenshotsBidirectional(game, remoteFull, settings);

                                    RommGameHelpers.SaveSyncHashes(game, remoteHash);
                                }

                                hasChanges = true;

                            }

                            completedGames++;

                            if (hasChanges)
                            {
                                saveCounter++;
                                if (saveCounter >= settings.SaveBatchSize)
                                {
                                    dataManager.Save();
                                    hasChanges = false;
                                    saveCounter = 0;
                                }
                            }
                        }

                        if (hasChanges)
                        {
                            dataManager.Save();
                            hasChanges = false;
                        }

                        var localGamesFromPlatform = dataManager.GetAllGames()
                            .Where(g =>
                                g.Platform != null &&
                                g.Platform.StartsWith(RommConstants.PlatformPrefix) &&
                                g.GetAllCustomFields()
                                 .Any(f => f.Name == GameCustomFields.PlatformId && f.Value == rommPlatform.Id.ToString()))
                            .ToList();

                        foreach (var localGame in localGamesFromPlatform)
                        {
                            var rommId = RommGameHelpers.GetRommId(localGame);

                            if (rommId == 0)
                            {
                                continue;
                            }

                            if (!serverGameIds.Contains(rommId))
                            {
                                dataManager.TryRemoveGame(localGame);
                                removedGames.Add(localGame);
                                RommLogger.Log($"Game {rommId} removed from platform '{platform.Name}' (not in server)");
                                hasChanges = true;

                            }
                        }

                    }

                    if (newPlatforms.Any())
                    {
                        if (!headless)
                        {
                            using (var form = new ConfirmForm(
                                string.Format(LocaleManager.Get("sync.new_platforms"), string.Join("\r\n", newPlatforms))))
                            {
                                var result = form.ShowDialog();
                                if (result != System.Windows.Forms.DialogResult.OK)
                                {
                                    newPlatforms.Clear();
                                    hasChanges = false;
                                }
                            }
                        }
                    }

                    if (hasChanges)
                    {
                        _backupService.BackupXml(RommConstants.PlatformsFile);
                        dataManager.Save();
                        hasChanges = false;
                    }

                    if (settings.ForceFullResync)
                    {
                        settings.ForceFullResync = false;
                        RommPluginStorage.Save(settings);
                    }

                    if (settings.ForcePushToServer)
                    {
                        settings.ForcePushToServer = false;
                        RommPluginStorage.Save(settings);
                    }

                    rommGamesOnly = dataManager.GetAllGames()
                        .Where(g => g.Platform != null && g.Platform.StartsWith(RommConstants.PlatformPrefix))
                        .ToList();

                    if (newPlatforms.Count > 0)
                    {
                        _hierarchyCli.LaunchHierarchyCli(platforms, rommGamesOnly, platformCategoryMap, false);

                        try
                        {
                            var baseDirForCleanup = RommPaths.PluginFolder;
                            var platformsDirForCleanup = Path.GetFullPath(Path.Combine(baseDirForCleanup, "..", "..", "Data", "Platforms"));
                            var categoriesWithGamesFromXml = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            if (Directory.Exists(platformsDirForCleanup))
                            {
                                foreach (var file in Directory.GetFiles(platformsDirForCleanup, "RomM _ *.xml"))
                                {
                                    try
                                    {
                                        var platformName = RommConstants.PlatformPrefix + Path.GetFileNameWithoutExtension(file).Substring(RommConstants.PlaylistPrefix.Length);
                                        var doc = System.Xml.Linq.XDocument.Load(file);
                                        if (doc.Root?.Elements("Game").Any() == true)
                                        {
                                            if (platformCategoryMap.TryGetValue(platformName, out var cat))
                                                categoriesWithGamesFromXml.Add(cat);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        RommLogger.LogError($"Failed to read platform file for cleanup: {ex.Message}");
                                    }
                                }
                            }

                            var rommCategories = dataManager.GetAllPlatformCategories()
                                .Where(c => c.Name != null && c.Name.StartsWith(RommConstants.PlatformPrefix) && c.Name != RommConstants.RootCategoryName)
                                .ToList();

                            foreach (var cat in rommCategories)
                            {
                                if (!categoriesWithGamesFromXml.Contains(cat.Name))
                                {
                                    dataManager.TryRemovePlatformCategory(cat);
                                    RommLogger.Log($"Removed empty RomM category: {cat.Name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            RommLogger.Log($"Category cleanup error: {ex.Message}");
                        }
                    }

                    foreach (var removedGame in removedGames)
                    {
                        try
                        {
                            _screenshotSync.DeleteGameImages(removedGame);
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to delete images for {removedGame.Title}: {ex.Message}");
                        }
                    }

                    RommLogger.Log($"Sync completed. Changes saved: {hasChanges}");

                    if (!headless && newPlatforms.Count > 0 && !sameSelection)
                    {
                        using (var form = new RestartConfirmForm())
                        {
                            var result = form.ShowDialog();
                            if (result == System.Windows.Forms.DialogResult.Yes)
                            {
                                _hierarchyCli.LaunchHierarchyCli(platforms, rommGamesOnly, platformCategoryMap, true);
                            }
                            else
                            {
                            }
                        }
                    }
                }
                );
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Sync error: {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateServerMetadata(string username, string password, string clientApiToken = null)
        {
            await ProgressRunner.RunAsync(
                "Reset Metadata in RomM server...",
                async progress =>
                {
                    if (!string.IsNullOrWhiteSpace(clientApiToken))
                    {
                        _api.SetBearerAuthentication(clientApiToken.Trim());
                    }
                    else
                    {
                        _api.SetBasicAuthentication(username, password);
                    }

                    var dataManager = PluginHelper.DataManager;

                    var allRommPlatforms = await _api.GetPlatformsAsync();
                    if (allRommPlatforms == null || allRommPlatforms.Count == 0)
                    {
                        using (var form = new ConfirmForm(LocaleManager.Get("sync.no_platforms")))
                        {
                            form.ShowDialog();
                        }
                        return;
                    }

                    var list = allRommPlatforms
                        .Select(p => new PlatformSelection
                        {
                            Id = p.Id,
                            Name = $"RomM | {(string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName)}",
                            Selected = true
                        })
                        .ToList();

                    using (var form = new RommPlatformSelectorForm(list))
                    {
                        if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }
                    }

                    var selectedPlatforms = list
                        .Where(p => p.Selected)
                        .Select(p => p.Name)
                        .ToHashSet();

                    var rommGamesOnly = dataManager.GetAllGames()
                            .Where(g => g.Platform != null && selectedPlatforms.Contains(g.Platform))
                            .ToList();

                    if (rommGamesOnly == null || rommGamesOnly.Count == 0)
                    {
                        using (var form = new ConfirmForm(LocaleManager.Get("sync.no_games")))
                        {
                            form.ShowDialog();
                        }
                        return;
                    }

                    RommLogger.Log($"Update metadata started: {rommGamesOnly.Count} games");

                    var completedGames = 0;
                    var gamesTotal = rommGamesOnly.Count;
                    var progressLock = new object();
                    var failedGames = new List<string>();

                    progress.SetTitle($"RomM: Update all metadata");

                    using (var semaphore = new SemaphoreSlim(5))
                    {
                    var tasks = rommGamesOnly.Select(async game =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            if (!RommGameHelpers.TryGetRommId(game, out int rommId) || rommId <= 0)
                            {
                                RommLogger.Log($"Skipping game '{game.Title}': no valid RomM ID");
                                return;
                            }

                            var artworkPath = _screenshotSync.GetCoverImagePath(game);
                            var originalArtwork = artworkPath;

                            if (!string.IsNullOrEmpty(artworkPath) && File.Exists(artworkPath))
                            {
                                artworkPath = RommImageService.EnsureRgbJpeg(artworkPath);
                            }

                            var request = new RommUpdateGameRequest
                            {
                                Name = game.Title,
                                Summary = game.Notes,
                                LaunchboxId = game.LaunchBoxDbId,
                                RawLaunchboxMetadata = LaunchboxMetadataService.BuildLaunchboxMetadata(game),
                                ArtworkPath = artworkPath
                            };

                            try
                            {
                                await _api.UpdateGameById(rommId, request);
                                RommLogger.Log($"Game {rommId} metadata updated on server");
                            }
                            catch (Exception ex)
                            {
                                var platform = game.Platform ?? "Unknown";
                                var gameName = $"{platform}/{game.Title} (RomM ID: {rommId})";
                                RommLogger.LogException(ex);
                                lock (progressLock) { failedGames.Add(gameName); }
                            }

                            if (!string.IsNullOrEmpty(artworkPath) && artworkPath != originalArtwork)
                            {
                                try { File.Delete(artworkPath); } catch { }
                            }

                            var done = Interlocked.Increment(ref completedGames);
                            if (done % 10 == 0)
                            {
                                lock (progressLock)
                                {
                                    progress.SetStatus($"Games: {done} of {gamesTotal}");
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                    }

                    RommLogger.Log($"Update metadata completed: {gamesTotal} games");

                    if (failedGames.Count > 0)
                    {
                        RommLogger.LogError($"Update failed for {failedGames.Count} game(s). Check the log file for details.");
                        using (var form = new ConfirmForm(
                            string.Format(LocaleManager.Get("sync.failed"), failedGames.Count)))
                        {
                            form.ShowDialog();
                        }
                    }
                    else
                    {
                        using (var form = new ConfirmForm(LocaleManager.Get("sync.all_metadata_updated")))
                        {
                            form.ShowDialog();
                        }
                    }
                }
            );
        }

        /// <inheritdoc/>
        public void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings)
        {
            _metadataMapper.ApplyServerMetadata(game, rommGame, settings);
        }

        /// <inheritdoc/>
        public async Task PushGameMetadataAsync(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            RommGameHelpers.TryGetRommId(game, out int rommId);
            if (rommId == 0) return;

            var artworkPath = _screenshotSync.GetCoverImagePath(game);
            var originalArtwork = artworkPath;

            if (!string.IsNullOrEmpty(artworkPath) && File.Exists(artworkPath))
            {
                artworkPath = RommImageService.EnsureRgbJpeg(artworkPath);
            }

            var request = new RommUpdateGameRequest
            {
                Name = game.Title,
                Summary = game.Notes,
                LaunchboxId = game.LaunchBoxDbId,
                RawLaunchboxMetadata = LaunchboxMetadataService.BuildLaunchboxMetadata(game),
                ArtworkPath = artworkPath
            };

            try
            {
                await _api.UpdateGameById(rommId, request);
                RommLogger.Log($"Game {rommId} metadata pushed to server: {game.Title}");
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
            finally
            {
                if (!string.IsNullOrEmpty(artworkPath) && artworkPath != originalArtwork && File.Exists(artworkPath))
                {
                    try { File.Delete(artworkPath); } catch { }
                }
            }

            await _screenshotSync.SyncScreenshotsBidirectional(game, remoteGame, settings);
        }

        /// <inheritdoc/>
        public async Task SyncStatsOnGameLaunch(IGame game, int rommId)
        {
            await _statsService.SyncStatsOnGameLaunch(game, rommId);
        }

        /// <inheritdoc/>
        public async Task SyncStatsOnGameExit(IGame game, int rommId, DateTime startTime)
        {
            await _statsService.SyncStatsOnGameExit(game, rommId, startTime);
        }

        /// <inheritdoc/>
        public async Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            await _screenshotSync.SyncScreenshotsBidirectional(game, remoteGame, settings);
        }

        /// <inheritdoc/>
        public async Task<RommStats> FetchLatestStatsFromRomm(int romId)
        {
            return await _statsService.FetchLatestStatsFromRomm(romId);
        }

        /// <inheritdoc/>
        public void CompareAndUpdateStats(IGame game, RommStats rommStats)
        {
            _statsService.CompareAndUpdateStats(game, rommStats);
        }

        /// <inheritdoc/>
        public async Task SendPlaySessionToRomm(int rommGameId, DateTime startTime, DateTime endTime, long durationMs)
        {
            await _statsService.SendPlaySessionToRomm(rommGameId, startTime, endTime, durationMs);
        }
    }
}
