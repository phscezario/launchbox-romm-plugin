using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RommPlugin.ApiClient;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using Microsoft.Extensions.DependencyInjection;

namespace RommPlugin
{
    public class RommMenuPlugin : ISystemEventsPlugin, IGameLaunchingPlugin
    {
        private DateTime? _gameStartTime;
        private IGame _currentGame;
        private int _currentRommId;
        private readonly object _gameLock = new object();

        public async void OnEventRaised(string eventType)
        {
            try
            {
                var pluginFolder = RommPaths.PluginFolder;

                if (eventType != SystemEventTypes.LaunchBoxStartupCompleted)
                {
                    return;
                }

                var settings = RommPluginStorage.Load();

                RommLogger.Initialize(settings.SaveLogs, settings.LogRetentionDays);

                ServiceLocator.Initialize(services =>
                {
                    var installedGamesPath = System.IO.Path.Combine(RommPaths.PluginFolder, RommPlugin.Core.Constants.RommConstants.InstalledGamesFile);

                    services.AddSingleton<IRommApiClient>(sp =>
                    {
                        var s = RommPluginStorage.Load();
                        return new RommApiClient(s.RommBaseUrl ?? "http://localhost");
                    });
                    services.AddSingleton<IDownloadQueueService>(sp =>
                    {
                        var s = RommPluginStorage.Load();
                        return new DownloadQueueService(RommPaths.DownloadStateFile, s.RomsPath ?? "", s.RommBaseUrl ?? "http://localhost");
                    });
                    services.AddSingleton<IInstalledGamesService>(sp =>
                        new InstalledGamesService(installedGamesPath));
                    services.AddSingleton<IRommMetadataMapper, RommMetadataMapper>();
                    services.AddSingleton<IRommBackupService, RommBackupService>();
                    services.AddSingleton<IRommHierarchyCli, RommHierarchyCli>();
                    services.AddSingleton<IRommScreenshotSync>(sp =>
                        new RommScreenshotSync(sp.GetRequiredService<IRommApiClient>()));
                    services.AddSingleton<IRommStatsService>(sp =>
                        new RommStatsService(sp.GetRequiredService<IRommApiClient>()));
                    services.AddSingleton<IRommSyncService, RommSyncService>();
                    services.AddSingleton<IRommResetServerService, RommResetServerService>();
                    services.AddSingleton<IRommProcessInstallUninstallService, RommProcessInstallUninstallService>();
                    services.AddSingleton<RommConnectionTester>();
                });

                var localeFolder = RommPaths.LocalesFolder;
                try
                {
                    LocaleManager.Initialize(localeFolder, settings.Language ?? "en");
                }
                catch
                {
                }

                SessionSuppressStorage.Delete();

                if (GitHubUpdateService.HasPendingUpdate())
                {
                    ApplyPendingUpdateOnStartup();
                    return;
                }

                if (settings.AutoUpdateEnabled)
                {
                    _ = CheckAndUpdateAsync(settings);
                }

                if (settings.ProcessPendingOnStartup)
                {
                    try
                    {
                        var stateFilePath = RommPaths.DownloadStateFile;

                        if (File.Exists(stateFilePath))
                        {
                            var stateJson = File.ReadAllText(stateFilePath);
                            var state = Newtonsoft.Json.JsonConvert.DeserializeObject<RommPlugin.Core.Models.DownloadState>(stateJson);
                            var hasPending = state?.Items?.Any(
                                i => i.Status == RommPlugin.Core.Models.DownloadStatus.WaitingInstall ||
                                     i.Status == RommPlugin.Core.Models.DownloadStatus.WaitingUninstall) == true;

                            if (hasPending)
                            {
                                RommPlugin.MenuItems.Buttons.RommGameManagerMenuItem.OpenOrBringToFront();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RommLogger.LogError("[RommPlugin] Startup pending check error: " + ex);
                    }
                }

                try
                {
                    var lastAutoSync = GetLastAutoSyncAt();

                    if (settings.AutoSyncIntervalDays == -1)
                    {
                        RommLogger.Log("Auto sync disabled (interval = -1)");
                    }
                    else
                    {
                    var hasSelection = settings.LastSelectedPlatformIds != null && settings.LastSelectedPlatformIds.Count > 0;

                    var shouldSync = hasSelection && (
                        settings.AutoSyncIntervalDays == 0
                        || lastAutoSync == null
                        || (DateTime.UtcNow - lastAutoSync.Value).TotalDays >= settings.AutoSyncIntervalDays);

                    if (shouldSync)
                    {
                        if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                        {
                            RommLogger.Log("Auto sync skipped: RomM base URL not configured");
                        }
                        else
                        {
                        RommLogger.Log("Auto sync triggered on startup");
                        var autoSyncApi = (RommApiClient)ServiceLocator.GetService<IRommApiClient>();
                        autoSyncApi.ApplyAuthentication(settings);
                        var autoSyncService = ServiceLocator.GetService<IRommSyncService>();
                        autoSyncService.SetApi(autoSyncApi);
                        await autoSyncService.SyncAsync(headless: true);
                        SaveLastAutoSyncAt(DateTime.UtcNow);
                        }
                    }
                    }
                }
                catch (Exception ex)
                {
                    RommLogger.LogError("[RommPlugin] Auto sync error: " + ex);
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError("[RommPlugin] Unhandled error in OnEventRaised: " + ex);
            }
        }

        public void OnBeforeGameLaunching(IGame game, IAdditionalApplication app, IEmulator emulator)
        {
            try
            {
                var settings = RommPluginStorage.Load();
                if (!settings.UpdateStatsOnGameLaunch)
                {
                    return;
                }

                if (game == null)
                {
                    return;
                }

                var fields = game.GetAllCustomFields().GroupBy(f => f.Name).ToDictionary(g => g.Key, g => g.Last().Value);
                if (!fields.TryGetValue(GameCustomFields.GameId, out var rommIdStr) || !int.TryParse(rommIdStr, out var rommId))
                {
                    return;
                }

                lock (_gameLock)
                {
                    _gameStartTime = DateTime.UtcNow;
                    _currentGame = game;
                    _currentRommId = rommId;
                }

                RommLogger.Log($"Game starting: {game.Title} (RomM ID: {rommId})");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error in OnBeforeGameLaunching: {ex.Message}");
            }
        }

        public async void OnAfterGameLaunched(IGame game, IAdditionalApplication app, IEmulator emulator)
        {
            IGame currentGame;
            int currentRommId;

            try
            {
                var settings = RommPluginStorage.Load();
                if (!settings.UpdateStatsOnGameLaunch)
                {
                    return;
                }

                lock (_gameLock)
                {
                    if (_currentGame == null || _currentRommId == 0)
                    {
                        return;
                    }

                    currentGame = _currentGame;
                    currentRommId = _currentRommId;
                }

                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    return;
                }

                var api = (RommApiClient)ServiceLocator.GetService<IRommApiClient>();
                try
                {
                    api.ApplyAuthentication(settings);

                    var syncService = (RommSyncService)ServiceLocator.GetService<IRommSyncService>();
                    syncService.SetApi(api);

                    await syncService.SyncStatsOnGameLaunch(currentGame, currentRommId);
                    PluginHelper.DataManager.Save();
                    var remoteGame = await api.GetGameByIdAsync(currentRommId);
                    if (remoteGame != null)
                    {
                        await syncService.SyncScreenshotsBidirectional(currentGame, remoteGame, settings);
                    }
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"Error in OnAfterGameLaunched: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error in OnAfterGameLaunched: {ex.Message}");
            }
        }

        public async void OnGameExited()
        {
            IGame game;
            int rommId;
            DateTime gameStart;

            try
            {
                var settings = RommPluginStorage.Load();
                if (!settings.UpdateStatsOnGameLaunch)
                {
                    return;
                }

                lock (_gameLock)
                {
                    if (_currentGame == null || _currentRommId == 0 || !_gameStartTime.HasValue)
                    {
                        return;
                    }

                    game = _currentGame;
                    rommId = _currentRommId;
                    gameStart = _gameStartTime.Value;
                }

                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    return;
                }

                var api = (RommApiClient)ServiceLocator.GetService<IRommApiClient>();
                try
                {
                    api.ApplyAuthentication(settings);

                    var syncService = (RommSyncService)ServiceLocator.GetService<IRommSyncService>();
                    syncService.SetApi(api);

                    await syncService.SyncStatsOnGameExit(game, rommId, gameStart);
                    PluginHelper.DataManager.Save();
                    var remoteGame = await api.GetGameByIdAsync(rommId);
                    if (remoteGame != null)
                    {
                        await syncService.SyncScreenshotsBidirectional(game, remoteGame, settings);
                    }
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"Error in OnGameExited: {ex.Message}");
                }

                RommLogger.Log($"Game exited: {game.Title} (RomM ID: {rommId})");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error in OnGameExited: {ex.Message}");
            }
            finally
            {
                lock (_gameLock)
                {
                    _gameStartTime = null;
                    _currentGame = null;
                    _currentRommId = 0;
                }
            }
        }

        private async Task CheckAndUpdateAsync(RommPluginSettings settings)
        {
            try
            {
                var result = await GitHubUpdateService.CheckForUpdateAsync();

                if (!result.UpdateAvailable)
                    return;

                var version = result.LatestVersion.ToString(3);
                var currentVersion = result.CurrentVersion.ToString(3);

                var message = string.Format(LocaleManager.Get("update.available"), version, currentVersion);

                if (!string.IsNullOrEmpty(result.ReleaseNotes))
                {
                    var notes = result.ReleaseNotes.Length > 500
                        ? result.ReleaseNotes.Substring(0, 500) + "..."
                        : result.ReleaseNotes;
                    message += string.Format(LocaleManager.Get("update.release_notes"), notes);
                }

                var dialogResult = MessageBox.Show(
                    message,
                    LocaleManager.Get("update.title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (dialogResult != MessageBoxResult.Yes)
                    return;

                var asset = result.ZipAsset ?? result.SetupAsset;
                if (asset == null)
                {
                RommLogger.Log("No downloadable asset found for update");
                    return;
                }

                RommLogger.Log("Downloading update: " + asset.Name);
                var downloaded = await GitHubUpdateService.DownloadUpdateAsync(asset, version);

                if (!downloaded)
                {
                    MessageBox.Show(
                        LocaleManager.Get("update.download_failed"),
                        LocaleManager.Get("update.error_title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var restartMessage = string.Format(LocaleManager.Get("update.downloaded"), version);

                var restartResult = MessageBox.Show(
                    restartMessage,
                    LocaleManager.Get("update.downloaded_title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (restartResult == MessageBoxResult.Yes)
                {
                    RommLogger.Log("User chose to restart now");
                    GitHubUpdateService.ApplyPendingUpdate();
                }
                else
                {
                    RommLogger.Log("User chose to apply later. Update will be applied on next startup.");
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError("Update check failed: " + ex.Message);
            }
        }

        private void ApplyPendingUpdateOnStartup()
        {
            try
            {
                var version = GitHubUpdateService.GetPendingVersion();
                var message = string.Format(LocaleManager.Get("update.pending_message"), version);

                var result = MessageBox.Show(
                    message,
                    LocaleManager.Get("update.pending_title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    RommLogger.Log("Applying pending update: " + version);
                    GitHubUpdateService.ApplyPendingUpdate();
                }
                else
                {
                    RommLogger.Log("User deferred pending update. Will apply on next startup.");
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError("Failed to apply pending update: " + ex.Message);
                GitHubUpdateService.CleanupUpdateDir();
            }
        }

        private DateTime? GetLastAutoSyncAt()
        {
            try
            {
                var settings = RommPluginStorage.Load();
                return settings.LastAutoSyncAt;
            }
            catch
            {
            }

            return null;
        }

        private void SaveLastAutoSyncAt(DateTime dateTime)
        {
            try
            {
                var settings = RommPluginStorage.Load();
                settings.LastAutoSyncAt = dateTime;
                RommPluginStorage.Save(settings);
            }
            catch
            {
            }
        }
    }
}
