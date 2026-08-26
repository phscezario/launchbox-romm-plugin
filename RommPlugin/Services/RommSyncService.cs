using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using RommPlugin.ApiClient;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using RommPlugin.UI.Forms;
using RommPlugin.UI.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public class RommSyncService
    {
        private RommApiClient _api;
        private static int _isRunning = 0;

        public RommApiClient Api => _api;

        public void SetApi(RommApiClient api)
        {
            _api = api;
        }

        public async Task SyncAsync(bool headless = false)
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                RommLogger.Log("[DIAG] Sync already running, skipping");
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
                        .Where(p => p.Name != null && p.Name.StartsWith("RomM | "))
                        .ToList();

                    var rommGamesOnly = dataManager.GetAllGames()
                        .Where(g => g.Platform != null && g.Platform.StartsWith("RomM | "))
                        .ToList();

                    var platformCategories = dataManager.GetAllPlatformCategories()
                        .Where(c => c.Name != null && c.Name.StartsWith("RomM | "))
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
                        platformCategoryMap[pname] = $"RomM | {ParseCategory(rp.Category)}";
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

                    var newPlatforms = new List<string>();
                    var removedGames = new List<IGame>();

                    var rootRommCategory = platformCategories.FirstOrDefault(p => p.Name == "RomM");
                    if (rootRommCategory == null)
                    {
                        try
                        {
                            rootRommCategory = dataManager.AddNewPlatformCategory("RomM");
                            platformCategories.Add(rootRommCategory);
                            hasChanges = true;
                            RommLogger.Log("Created root 'RomM' platform category in Platforms.xml");
                        }
                        catch { }
                    }

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        progress.CancellationToken.ThrowIfCancellationRequested();

                        var name = !string.IsNullOrWhiteSpace(rommPlatform.CustomName)
                            ? rommPlatform.CustomName
                            : rommPlatform.Name;

                        var platformName = $"RomM | {name}";

                        var platform = platforms
                            .FirstOrDefault(p => string.Equals(p.Name, platformName, StringComparison.OrdinalIgnoreCase));

                        if (platform == null)
                        {
                            var parsedCategory = ParseCategory(rommPlatform.Category);
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

                            newPlatforms.Add(platformName);
                        }

                        if (!selectedPlatformIds.Contains(rommPlatform.Id))
                        {
                            continue;
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
                                var savedLocalHash = GetCustomField(existingGame, GameCustomFields.LocalMetadataHash);
                                var savedRemoteHash = GetCustomField(existingGame, GameCustomFields.RemoteMetadataHash);

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

                                        SetCustomField(existingGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                                        SetCustomField(existingGame, GameCustomFields.LocalMetadataHash,
                                            RommMetadataComparer.ComputeLocalMetadataHash(existingGame));
                                        SetCustomField(existingGame, GameCustomFields.RemoteMetadataHash, remoteHash);
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
                

                                            SetCustomField(existingGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                                            SetCustomField(existingGame, GameCustomFields.LocalMetadataHash,
                                                RommMetadataComparer.ComputeLocalMetadataHash(existingGame));
                                            SetCustomField(existingGame, GameCustomFields.RemoteMetadataHash, remoteHash);
                                        }
                                        catch (Exception ex)
                                        {
                                            RommLogger.LogError($"Failed to push metadata for game {rommGame.Id}: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        hasChanges = true;
            

                                        SetCustomField(existingGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                                        SetCustomField(existingGame, GameCustomFields.LocalMetadataHash,
                                            RommMetadataComparer.ComputeLocalMetadataHash(existingGame));
                                        SetCustomField(existingGame, GameCustomFields.RemoteMetadataHash, remoteHash);
                                    }
                                }
                                else
                                {
                                    ApplyServerMetadata(existingGame, remoteFull, settings);

                                    if (!HasAnyBoxFrontImage(existingGame))
                                    {
                                        await DownloadAndSetCoverArt(existingGame, remoteFull);
                                    }

                                    await SyncScreenshotsBidirectional(existingGame, remoteFull, settings);
                                    hasChanges = true;
        

                                    SetCustomField(existingGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                                    SetCustomField(existingGame, GameCustomFields.LocalMetadataHash,
                                        RommMetadataComparer.ComputeLocalMetadataHash(existingGame));
                                    SetCustomField(existingGame, GameCustomFields.RemoteMetadataHash, remoteHash);
                                }
                            }
                            else
                            {
                                var normalizedTitle = NormalizeGameTitle(rommGame.Name);
                                RommLogger.Log($"Game {rommGame.Id} created: {normalizedTitle}");
                                var game = dataManager.AddNewGame(normalizedTitle);

                                game.Platform = platform.Name;

                                var isFolderGame = rommGame.HasMultipleFiles;

                                SetCustomField(game, GameCustomFields.GameId, rommGame.Id.ToString());
                                SetCustomField(game, GameCustomFields.PlatformId, rommPlatform.Id.ToString());
                                SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
                                SetCustomField(game, GameCustomFields.FileName, rommGame.FsName);
                                SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());

                                game.Installed = game.Installed != null ? game.Installed : false;

                                localGamesById[rommGame.Id] = game;

                                var remoteFull = await _api.GetGameByIdAsync(rommGame.Id);
                                if (remoteFull != null)
                                {
                                    ApplyServerMetadata(game, remoteFull, settings);

                                    if (!HasAnyBoxFrontImage(game))
                                    {
                                        await DownloadAndSetCoverArt(game, remoteFull);
                                    }

                                    await SyncScreenshotsBidirectional(game, remoteFull, settings);

                                    SetCustomField(game, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                                    SetCustomField(game, GameCustomFields.LocalMetadataHash,
                                        RommMetadataComparer.ComputeLocalMetadataHash(game));
                                    SetCustomField(game, GameCustomFields.RemoteMetadataHash, remoteHash);
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
                                g.Platform.StartsWith("RomM | ") &&
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
                                    RommLogger.Log($"[DIAG] User cancelled new platforms. Skipping restart.");
                                }
                            }
                        }
                    }

                    if (hasChanges)
                    {
                        BackupXml("Platforms.xml");
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
                        .Where(g => g.Platform != null && g.Platform.StartsWith("RomM | "))
                        .ToList();

                    if (newPlatforms.Count > 0)
                    {
                        LaunchHierarchyCli(platforms, rommGamesOnly, platformCategoryMap, false);

                        // Limpar categorias RomM sem jogos (baseado nos XML files, não HasGames)
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
                                        var platformName = "RomM | " + Path.GetFileNameWithoutExtension(file).Substring("RomM _ ".Length);
                                        var doc = XDocument.Load(file);
                                        if (doc.Root?.Elements("Game").Any() == true)
                                        {
                                            if (platformCategoryMap.TryGetValue(platformName, out var cat))
                                                categoriesWithGamesFromXml.Add(cat);
                                        }
                                    }
                                    catch { }
                                }
                            }

                            var rommCategories = dataManager.GetAllPlatformCategories()
                                .Where(c => c.Name != null && c.Name.StartsWith("RomM | ") && c.Name != "RomM")
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
                            DeleteGameImages(removedGame);
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to delete images for {removedGame.Title}: {ex.Message}");
                        }
                    }

                    RommLogger.Log($"Sync completed. Changes saved: {hasChanges}");

                    if (!headless && newPlatforms.Count > 0 && !sameSelection)
                    {
                        RommLogger.Log($"[DIAG] Showing RestartConfirmForm...");
                        using (var form = new RestartConfirmForm())
                        {
                            var result = form.ShowDialog();
                            RommLogger.Log($"[DIAG] RestartConfirmForm result: {result}");
                            if (result == System.Windows.Forms.DialogResult.Yes)
                            {
                                RommLogger.Log($"[DIAG] User chose restart. Calling LaunchHierarchyCli with restart=true");
                                LaunchHierarchyCli(platforms, rommGamesOnly, platformCategoryMap, true);
                            }
                            else
                            {
                                RommLogger.Log($"[DIAG] User chose later (or closed form)");
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

        private string NormalizeGameTitle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var cleaned = name;

            while (true)
            {
                var ext = Path.GetExtension(cleaned);
                if (string.IsNullOrEmpty(ext) || !KnownExtensions.Extensions.Contains(ext))
                {
                    break;
                }

                cleaned = Path.GetFileNameWithoutExtension(cleaned);
            }

            return cleaned.Trim();
        }

        private string ParseCategory(string category)
        {
            switch (category)
            {
                case "Arcade":
                    return "Arcade";
                case "Console":
                    return "Consoles";
                case "Operating System":
                    return "Computers";
                case "Portable Console":
                    return "Handhelds";
                default:
                    return "Others";
            }
        }

        private void LaunchHierarchyCli(List<IPlatform> platforms, List<IGame> rommGamesOnly, Dictionary<string, string> platformCategoryMap, bool restartLaunchBox = false)
        {
            var baseDir = RommPaths.PluginFolder;
            var platformsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Data", "Platforms"));

            var platformsWithGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var categoryPlatforms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(platformsDir))
            {
                foreach (var file in Directory.GetFiles(platformsDir, "RomM _ *.xml"))
                {
                    try
                    {
                        var platformName = "RomM | " + Path.GetFileNameWithoutExtension(file)
                            .Substring("RomM _ ".Length);

                        var doc = XDocument.Load(file);
                        var gameCount = doc.Root?.Elements("Game").Count() ?? 0;

                        if (gameCount > 0)
                            platformsWithGames.Add(platformName);

                        if (platformCategoryMap.TryGetValue(platformName, out var category))
                        {
                            if (!categoryPlatforms.ContainsKey(category))
                                categoryPlatforms[category] = new List<string>();
                            categoryPlatforms[category].Add(platformName);
                        }
                    }
                    catch (Exception ex)
                    {
                        RommLogger.Log($"Hierarchy CLI: failed to read platform file '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }
            }

            if (categoryPlatforms.Count == 0)
            {
                RommLogger.Log($"[DIAG] LaunchHierarchyCli: EARLY RETURN - no RomM platforms found");
                return;
            }

            var allCategories = platformCategoryMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var categoriesWithGames = categoryPlatforms
                .Where(kv => kv.Value.Any(p => platformsWithGames.Contains(p)))
                .Select(kv => kv.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            RommLogger.Log($"[DIAG] LaunchHierarchyCli: platformsWithGames={platformsWithGames.Count}, allCategories={allCategories.Count}, categoriesWithGames={categoriesWithGames.Count}, categoryPlatformsKeys={categoryPlatforms.Count}, restart={restartLaunchBox}");
            foreach (var ps in platformsWithGames)
                RommLogger.Log($"[DIAG]   platformWithGame: '{ps}'");
            foreach (var kv in categoryPlatforms)
                RommLogger.Log($"[DIAG]   categoryPlatform: '{kv.Key}' -> [{string.Join(", ", kv.Value)}]");
            foreach (var c in allCategories)
                RommLogger.Log($"[DIAG]   allCategory: '{c}'");
            foreach (var c in categoriesWithGames)
                RommLogger.Log($"[DIAG]   categoriesWithGames: '{c}'");

            var launchBoxExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LaunchBox.exe"));
            var request = new
            {
                PlatformCategoryMap = platformCategoryMap,
                AllCategories = allCategories,
                CategoriesWithGames = categoriesWithGames,
                CategoryPlatforms = categoryPlatforms,
                RestartLaunchBox = restartLaunchBox,
                LaunchBoxExe = launchBoxExe
            };

            var json = JsonConvert.SerializeObject(request, Formatting.Indented);
            var pendingPath = Path.Combine(baseDir, "pending_hierarchy.json");

            File.WriteAllText(pendingPath, json);
            RommLogger.Log($"Hierarchy CLI: wrote pending file with {allCategories.Count} allCategories, {categoriesWithGames.Count} categoriesWithGames, {categoryPlatforms.Values.Sum(l => l.Count)} platforms, restart={restartLaunchBox}");

            var cliPath = Path.Combine(baseDir, "RommPlugin.CLI.exe");
            if (!File.Exists(cliPath))
            {
                RommLogger.LogError($"CLI not found at {cliPath}");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"\"{pendingPath}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                var proc = Process.Start(psi);

                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                foreach (var line in stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    RommLogger.Log($"CLI: {line}");

                if (!string.IsNullOrEmpty(stderr))
                    RommLogger.LogError($"CLI errors: {stderr}");

                if (proc.ExitCode != 0)
                    RommLogger.LogError($"CLI exited with code {proc.ExitCode}");

                RommLogger.Log($"Hierarchy CLI completed (restart={restartLaunchBox})");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to launch hierarchy CLI: {ex.Message}");
            }
        }

        private void SetCustomField(IGame game, string name, string value, bool overwrite = true)
        {
            var field = game.GetAllCustomFields().FirstOrDefault(f => f.Name == name);

            if (field == null)
            {
                field = game.AddNewCustomField();
                field.Name = name;
                field.Value = value;

                return;
            }

            if (!overwrite)
            {
                return;
            }

            field.Value = value;
        }

        private string GetCustomField(IGame game, string name)
        {
            if (game == null) return null;
            return game.GetAllCustomFields().FirstOrDefault(f => f.Name == name)?.Value;
        }

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

                            var artworkPath = GetCoverImagePath(game);
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

        private string GetCoverImagePath(IGame game)
        {
            var images = game.GetAllImagesWithDetails();

            foreach (var image in images)
            {
                if (image.ImageType == "Box - Front")
                {
                    return image.FilePath;
                }

                if (image.ImageType == "Fanart - Box - Front")
                {
                    return image.FilePath;
                }

                if (image.ImageType == "Advertisement Flyer - Front")
                {
                    return image.FilePath;
                }
            }

            return "";
        }

        private bool HasAnyBoxFrontImage(IGame game)
        {
            var images = game.GetAllImagesWithDetails();

            foreach (var image in images)
            {
                if (image.ImageType == "Box - Front")
                {
                    return true;
                }

                if (image.ImageType == "Fanart - Box - Front")
                {
                    return true;
                }

                if (image.ImageType == "Advertisement Flyer - Front")
                {
                    return true;
                }
            }

            return false;
        }

        public void ApplyServerMetadataPublic(IGame game, RommGame rommGame, RommPluginSettings settings)
        {
            ApplyServerMetadata(game, rommGame, settings);
        }

        public async Task PushGameMetadataAsyncPublic(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            await PushGameMetadataAsync(game, remoteGame, settings);
        }

        public async Task SyncScreenshotsBidirectionalPublic(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            await SyncScreenshotsBidirectional(game, remoteGame, settings);
        }

        private async Task PushGameMetadataAsync(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            RommGameHelpers.TryGetRommId(game, out int rommId);
            if (rommId == 0) return;

            var artworkPath = GetCoverImagePath(game);
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

            await SyncScreenshotsBidirectional(game, remoteGame, settings);
        }

        private void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings)
        {
            var shouldOverwrite = !settings.KeepLocalData;

            var launchboxMeta = rommGame.LaunchBoxMetadata;
            var ssMeta = rommGame.SsMetadata;
            var igdbMeta = rommGame.IgdbMetadata;
            var meta = rommGame.Metadatum;

            ApplyReleaseDate(game, launchboxMeta, ssMeta, igdbMeta, meta, shouldOverwrite);
            ApplyMaxPlayers(game, launchboxMeta, ssMeta, shouldOverwrite);
            ApplyStringField(game.ReleaseType, v => game.ReleaseType = v,
                launchboxMeta?.ReleaseType, null, null, null, shouldOverwrite);
            ApplyPlayMode(game, launchboxMeta, shouldOverwrite);
            ApplyVideoUrl(game, launchboxMeta, igdbMeta, shouldOverwrite);
            ApplyCommunityRating(game, launchboxMeta, igdbMeta, meta, shouldOverwrite);
            ApplyIntField(() => game.CommunityStarRatingTotalVotes, v => game.CommunityStarRatingTotalVotes = v,
                launchboxMeta?.CommunityRatingCount, null, null, null, shouldOverwrite);
            ApplyStringField(game.WikipediaUrl, v => game.WikipediaUrl = v,
                launchboxMeta?.WikipediaUrl, null, null, null, shouldOverwrite);
            ApplyStringField(game.Rating, v => game.Rating = v,
                launchboxMeta?.Esrb, null, null, null, shouldOverwrite);

            if (shouldOverwrite || string.IsNullOrEmpty(game.Notes))
            {
                game.Notes = ssMeta?.Synopsis ?? ssMeta?.Description ?? rommGame.Summary ?? game.Notes;
            }

            if (rommGame.LaunchboxId != null && rommGame.LaunchboxId > 0)
            {
                game.LaunchBoxDbId = rommGame.LaunchboxId;
            }
        }

        private void ApplyReleaseDate(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, IgdbMetadata igdb, RommGameMeta meta, bool overwrite)
        {
            if (overwrite || game.ReleaseDate == null)
            {
                DateTime? date = null;

                if (lb?.FirstReleaseDate != null)
                    date = UnixToDateTime(lb.FirstReleaseDate.Value);
                else if (ss?.ReleaseDate != null && DateTime.TryParse(ss.ReleaseDate, out var ssDate))
                    date = ssDate;
                else if (igdb?.FirstReleaseDate != null)
                    date = UnixToDateTime(igdb.FirstReleaseDate.Value);
                else if (meta?.FirstReleaseDate != null)
                    date = UnixToDateTime(meta.FirstReleaseDate.Value);

                if (date != null)
                    game.ReleaseDate = date.Value;
            }
        }

        private static DateTime UnixToDateTime(long value)
        {
            var dto = value > 100_000_000_000L
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return dto.DateTime;
        }

        private void ApplyMaxPlayers(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, bool overwrite)
        {
            if (overwrite || game.MaxPlayers == null || game.MaxPlayers == 0)
            {
                if (lb?.MaxPlayers != null)
                    game.MaxPlayers = lb.MaxPlayers.Value;
                else if (ss?.Players != null && int.TryParse(ss.Players, out var players))
                    game.MaxPlayers = players;
            }
        }

        private void ApplyPlayMode(IGame game, LaunchBoxMetadataModel lb, bool overwrite)
        {
            if (overwrite || string.IsNullOrEmpty(game.PlayMode))
            {
                if (lb?.Cooperative == true)
                    game.PlayMode = "Cooperative";
            }
        }

        private void ApplyVideoUrl(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, bool overwrite)
        {
            if (overwrite || string.IsNullOrEmpty(game.VideoUrl))
            {
                var videoId = lb?.YoutubeVideoId ?? igdb?.YoutubeVideoId;

                if (!string.IsNullOrEmpty(videoId))
                    game.VideoUrl = $"https://www.youtube.com/watch?v={videoId}";
            }
        }

        private void ApplyCommunityRating(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, RommGameMeta meta, bool overwrite)
        {
            if (overwrite || game.CommunityStarRating == 0)
            {
                if (lb?.CommunityRating > 0)
                    game.CommunityStarRating = lb.CommunityRating;
                else if (igdb?.TotalRating != null)
                    game.CommunityStarRating = (float)igdb.TotalRating.Value;
                else if (meta?.AverageRating != null)
                    game.CommunityStarRating = (float)meta.AverageRating.Value;
            }

            if (overwrite || game.CommunityStarRatingTotalVotes == 0)
            {
                if (lb?.CommunityRatingCount > 0)
                    game.CommunityStarRatingTotalVotes = lb.CommunityRatingCount;
            }
        }

        private void ApplyStringField(string currentValue, Action<string> setter,
            string lbValue, string ssValue, string igdbValue, string metaValue,
            bool shouldOverwrite)
        {
            if (shouldOverwrite || string.IsNullOrEmpty(currentValue))
            {
                var value = lbValue ?? ssValue ?? igdbValue ?? metaValue;

                if (!string.IsNullOrEmpty(value))
                    setter(value);
            }
        }

        private void ApplyIntField(Func<int> getter, Action<int> setter,
            int? lbValue, int? ssValue, int? igdbValue, int? metaValue,
            bool shouldOverwrite)
        {
            if (shouldOverwrite || getter() == 0)
            {
                var value = lbValue ?? ssValue ?? igdbValue ?? metaValue;

                if (value != null && value.Value > 0)
                    setter(value.Value);
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private async Task DownloadAndSetCoverArt(IGame game, RommGame rommGame)
        {
            var coverUrl = !string.IsNullOrEmpty(rommGame.PathCoverSmall)
                ? rommGame.PathCoverSmall
                : rommGame.UrlCover;

            if (!string.IsNullOrEmpty(coverUrl))
            {
                try
                {
                    var coverBytes = await _api.DownloadBytesAsync(coverUrl);

                    if (coverBytes == null || coverBytes.Length == 0)
                    {
                        RommLogger.Log($"Cover art download returned empty for {game.Title}");
                        return;
                    }

                    var imagePath = game.GetNextAvailableImageFilePath(".jpg", "Box - Front", null);
                    RommLogger.Log($"Cover art image path: {imagePath}");
                    EnsureDirectoryExists(imagePath);

                    var tempPath = Path.GetTempFileName();
                    try
                    {
                        File.WriteAllBytes(tempPath, coverBytes);
                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                        }
                        File.Move(tempPath, imagePath);
                    }
                    catch
                    {
                        try { File.Delete(tempPath); } catch { }
                        throw;
                    }

                    RommLogger.Log($"Cover art downloaded for {game.Title}: {imagePath}");
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"Failed to download cover for {game.Title}: {ex.Message}");
                }
            }
        }

        private void DeleteGameImages(IGame game)
        {
            var imagesFolder = RommHelpers.GetLaunchBoxImagesFolder();
            var platformFolder = game.Platform ?? "Unknown";
            var title = game.Title ?? "Unknown";

            var gameImagesDir = Path.Combine(imagesFolder, SanitizeFolderName(platformFolder), SanitizeFolderName(title));

            if (Directory.Exists(gameImagesDir))
            {
                Directory.Delete(gameImagesDir, true);
                RommLogger.Log($"Deleted images for removed game: {title}");
            }
        }

        private string SanitizeFolderName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return sanitized.Trim();
        }

        private void BackupXml(string fileName)
        {
            try
            {
                var dataDir = Path.GetFullPath(Path.Combine(RommPaths.PluginFolder, "..", "..", "Data"));
                var backupDir = Path.Combine(dataDir, "RomM_Backups");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var sourcePath = Path.Combine(dataDir, fileName);
                if (!File.Exists(sourcePath))
                    return;

                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"{baseName}_{timestamp}.xml");

                File.Copy(sourcePath, backupPath, true);
                RommLogger.Log($"Backup created: {Path.GetFileName(backupPath)}");

                var backups = Directory.GetFiles(backupDir, baseName + "_*.xml")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                while (backups.Count >= 5)
                {
                    var oldest = backups.Last();
                    backups.RemoveAt(backups.Count - 1);
                    try { File.Delete(oldest); } catch { }
                }
            }
            catch (Exception ex)
            {
                RommLogger.Log($"Backup warning for {fileName}: {ex.Message}");
            }
        }

        public async Task<RommStats> FetchLatestStatsFromRomm(int romId)
        {
            try
            {
                var sessions = await _api.GetPlaySessionsAsync(romId);

                if (sessions == null || sessions.Count == 0)
                {
                    return new RommStats();
                }

                return new RommStats
                {
                    PlayCount = sessions.Count,
                    TotalPlayTimeMs = sessions.Sum(s => s.DurationMs),
                    LastPlayed = sessions.Max(s => s.EndTime)
                };
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error fetching stats from RomM for rom {romId}: {ex.Message}");
                return new RommStats();
            }
        }

        public void CompareAndUpdateStats(IGame game, RommStats rommStats)
        {
            if (rommStats.LastPlayed == null)
            {
                return;
            }

            if (game.LastPlayedDate == null || rommStats.LastPlayed > game.LastPlayedDate)
            {
                game.PlayCount = rommStats.PlayCount;
                game.PlayTime = rommStats.TotalPlayTimeSeconds;
                game.LastPlayedDate = rommStats.LastPlayed;
                RommLogger.Log($"Updated stats for '{game.Title}': PlayCount={rommStats.PlayCount}, PlayTime={rommStats.TotalPlayTimeSeconds}s, LastPlayed={rommStats.LastPlayed}");
            }
        }

        public async Task SendPlaySessionToRomm(int rommGameId, DateTime startTime, DateTime endTime, long durationMs)
        {
            try
            {
                var payload = new PlaySessionIngestPayload
                {
                    DeviceId = "launchbox",
                    Sessions = new List<PlaySessionEntry>
                    {
                        new PlaySessionEntry
                        {
                            RomId = rommGameId,
                            StartTime = startTime.ToString("o"),
                            EndTime = endTime.ToString("o"),
                            DurationMs = durationMs
                        }
                    }
                };

                await _api.IngestPlaySessionsAsync(payload);
                await _api.UpdateGameLastPlayedAsync(rommGameId);
                RommLogger.Log($"Sent play session to RomM: romId={rommGameId}, duration={durationMs}ms");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error sending play session to RomM for rom {rommGameId}: {ex.Message}");
            }
        }

        public async Task SyncStatsOnGameLaunch(IGame game, int rommId)
        {
            try
            {
                var rommStats = await FetchLatestStatsFromRomm(rommId);
                CompareAndUpdateStats(game, rommStats);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error syncing stats on game launch: {ex.Message}");
            }
        }

        public async Task SyncStatsOnGameExit(IGame game, int rommId, DateTime startTime)
        {
            try
            {
                var durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                await SendPlaySessionToRomm(rommId, startTime, DateTime.UtcNow, durationMs);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error syncing stats on game exit: {ex.Message}");
            }
        }

        public async Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            try
            {
                if (remoteGame == null) return;

                var remoteScreenshots = remoteGame.UserScreenshots ?? new List<RommScreenshot>();
                var localImages = game.GetAllImagesWithDetails()
                    .Where(i => i.ImageType == "Screenshot")
                    .ToList();

                var localFileNames = new HashSet<string>(
                    localImages.Select(i => Path.GetFileNameWithoutExtension(i.FilePath)),
                    StringComparer.OrdinalIgnoreCase);

                var remoteFileNames = new HashSet<string>(
                    remoteScreenshots.Select(s => s.FileNameNoExt ?? Path.GetFileNameWithoutExtension(s.FileName ?? "")),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var localImage in localImages)
                {
                    var localName = Path.GetFileNameWithoutExtension(localImage.FilePath);
                    if (!remoteFileNames.Contains(localName) && File.Exists(localImage.FilePath))
                    {
                        try
                        {
                            var screenshotId = await _api.UploadScreenshotAsync(remoteGame.Id, localImage.FilePath);
                            if (screenshotId > 0 && settings.IsAdmin && settings.PublicScreenshots)
                            {
                                await _api.SetScreenshotPublicAsync(screenshotId);
                            }
                            RommLogger.Log($"Screenshot uploaded for game {remoteGame.Id}: {localName}");
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to upload screenshot {localName} for game {remoteGame.Id}: {ex.Message}");
                        }
                    }
                }

                foreach (var remoteScreenshot in remoteScreenshots)
                {
                    var remoteName = remoteScreenshot.FileNameNoExt
                        ?? Path.GetFileNameWithoutExtension(remoteScreenshot.FileName ?? "");

                    if (!string.IsNullOrEmpty(remoteName) && !localFileNames.Contains(remoteName))
                    {
                        try
                        {
                            var safeFileName = Path.GetFileName(remoteScreenshot.FileName ?? $"{remoteScreenshot.Id}.jpg");
                            var tempPath = Path.Combine(Path.GetTempPath(), safeFileName);
                            try
                            {
                                await _api.DownloadScreenshotAsync(remoteScreenshot.Id, tempPath);

                                if (File.Exists(tempPath))
                                {
                                    var imagePath = game.GetNextAvailableImageFilePath(".jpg", "Screenshot", null);
                                    EnsureDirectoryExists(imagePath);
                                    File.Copy(tempPath, imagePath, true);
                                    File.Delete(tempPath);
                                    RommLogger.Log($"Screenshot downloaded for game {remoteGame.Id}: {remoteName}");
                                }
                            }
                            catch
                            {
                                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                                throw;
                            }
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to download screenshot {remoteName} for game {remoteGame.Id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error syncing screenshots for game {remoteGame?.Id}: {ex.Message}");
            }
        }
    }
}