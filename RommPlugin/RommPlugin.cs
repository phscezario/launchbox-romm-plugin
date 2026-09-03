using System;
using System.IO;
using System.Linq;
using RommPlugin.ApiClient;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using RommPlugin.UI.Prompts;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using Microsoft.Extensions.DependencyInjection;

namespace RommPlugin
{
    /// <summary>
    /// Main plugin class that handles system events and game launching hooks for the RomM integration.
    /// </summary>
    public class RommMenuPlugin : ISystemEventsPlugin, IGameLaunchingPlugin
    {
        private DateTime? _gameStartTime;
        private IGame _currentGame;
        private int _currentRommId;
        private readonly object _gameLock = new object();

        /// <inheritdoc/>
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
                        var svc = new DownloadQueueService(RommPaths.DownloadStateFile, s.RomsPath ?? "", s.RommBaseUrl ?? "http://localhost");
                        svc.SetAuthentication(s.RommBaseUrl, s.ClientApiToken, s.Username, s.Password);
                        return svc;
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
                    new PluginUpdateOrchestrator(new WinFormsUpdatePrompts()).HandlePendingOnStartup();
                    return;
                }

                if (settings.AutoUpdateEnabled)
                {
                    _ = new PluginUpdateOrchestrator(new WinFormsUpdatePrompts()).CheckAndPromptOnStartupAsync();
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
                                RommPlugin.Services.GameManagerLauncher.EnsureOpen();
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
