using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using RommPlugin.ApiClient;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Forms;
using RommPlugin.UI.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public class RommSyncService
    {
        private RommApiClient _api;

        public void SetApi(RommApiClient api)
        {
            _api = api;
        }

        public async Task SyncAsync()
        {
            await ProgressRunner.RunAsync(
                "Starting sync from RomM...",
                async progress =>
                {
                    var settings = RommPluginStorage.Load();

                    _api.SetBasicAuthentication(settings.Username, settings.Password);

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

                    var rommPlatforms = await _api.GetPlatformsAsync();

                    if (rommPlatforms == null || rommPlatforms.Count == 0)
                    {
                        return;
                    }

                    var localGamesById = new Dictionary<int, IGame>();

                    foreach (var game in rommGamesOnly)
                    {
                        if (TryGetRommId(game, out var id))
                        {
                            localGamesById[id] = game;
                        }
                    }

                    var selectedPlatformIds = new HashSet<int>();

                    var list = rommPlatforms.Select(p => new PlatformSelection
                    {
                        Id = p.Id,
                        Name = string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName,
                        Selected = true
                    }).ToList();

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
                    }

                    var hasChanges = false;
                    var serverGameIds = new HashSet<int>();
                    var platformCompleted = 0;
                    var platformTotal = selectedPlatformIds.Count;

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        if (!selectedPlatformIds.Contains(rommPlatform.Id))
                        {
                            continue;
                        }

                        var parsedCategory = parseCategory(rommPlatform.Category);
                        var rommCategoryName = $"RomM | {parsedCategory}";

                        var rommCategory = platformCategories
                            .FirstOrDefault(p => p.Name == rommCategoryName);

                        if (rommCategory == null)
                        {
                            rommCategory = dataManager.AddNewPlatformCategory(rommCategoryName);
                            platformCategories.Add(rommCategory);
                            hasChanges = true;
                        }

                        var name = !string.IsNullOrWhiteSpace(rommPlatform.CustomName)
                            ? rommPlatform.CustomName
                            : rommPlatform.Name;

                        var platformName = $"RomM | {name}";

                        var platform = platforms
                            .FirstOrDefault(p => p.Name == platformName);

                        if (platform == null)
                        {
                            platform = dataManager.AddNewPlatform(platformName);
                            platform.Category = rommCategoryName;
                            platforms.Add(platform);
                            hasChanges = true;

                            settings.CurrentPlatforms.Add(new RommCurrentPlatform
                            {
                                Id = rommPlatform.Id,
                                Name = platformName
                            });
                        }

                        var rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);

                        if (rommGames == null)
                        {
                            continue;
                        }

                        progress.SetTitle($"RomM: Syncing {platform.Name}");

                        var completedGames = 0;
                        var totalGames = rommGames.Count;

                        foreach (var rommGame in rommGames)
                        {
                            progress.SetStatus($"Platform {platformCompleted}/{platformTotal} | Games {completedGames}/{totalGames}");

                            serverGameIds.Add(rommGame.Id);

                            if (localGamesById.TryGetValue(rommGame.Id, out var existingGame))
                            {
                                var overwriteLocalData = !settings.KeepLocalData;

                                if (overwriteLocalData)
                                {
                                    UpdateGame(existingGame, rommGame, platform.Name);
                                    hasChanges = true;
                                }
                            }
                            else
                            {
                                var normalizedTitle = NormalizeGameTitle(rommGame.Name);
                                var game = dataManager.AddNewGame(normalizedTitle);

                                game.Platform = platform.Name;

                                var isFolderGame = rommGame.Files.Count > 1;

                                SetCustomField(game, GameCustomFields.GameId, rommGame.Id.ToString());
                                SetCustomField(game, GameCustomFields.PlatformId, rommPlatform.Id.ToString());
                                SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
                                SetCustomField(game, GameCustomFields.FileName, (rommGame.FsName + (isFolderGame ? ".zip" : "")) ?? "");
                                SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());

                                AddInstallUninstallIfMissing(game, rommGame.Id);

                                game.Installed = game.Installed != null ? game.Installed : false;

                                localGamesById[rommGame.Id] = game;
                                hasChanges = true;
                            }

                            completedGames++;
                        }

                        platformCompleted++;
                    }

                    var localRommGames = localGamesById.Values.ToList();

                    foreach (var localGame in localRommGames)
                    {
                        var rommId = GetRommId(localGame);

                        if (!serverGameIds.Contains(rommId) && localGame.Platform?.StartsWith("RomM | ") == true)
                        {
                            dataManager.TryRemoveGame(localGame);
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        dataManager.Save();
                    }

                    CheckAndSavePlatforms(rommPlatforms, platforms);
                }
            );
        }

        private bool TryGetRommId(IGame game, out int rommId)
        {
            rommId = 0;

            var value = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out rommId);
        }

        private int GetRommId(IGame game)
        {
            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out var id) ? id : 0;
        }

        private static readonly HashSet<string> KnownExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".7z", ".rar",
            ".iso", ".cue", ".bin", ".img",
            ".chd", ".cso",
            ".nes", ".sfc", ".smc", ".gba",
            ".gb", ".gbc", ".n64", ".z64", ".v64",
            ".nds", ".3ds",
            ".gcz", ".nkit",
            ".xiso", ".xci", ".rvz",
            ".vpx", ".wad", ".wux"
        };

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
                if (string.IsNullOrEmpty(ext) || !KnownExtensions.Contains(ext))
                {
                    break;
                }

                cleaned = Path.GetFileNameWithoutExtension(cleaned);
            }

            return cleaned.Trim();
        }

        private void UpdateGame(IGame game, RommGame rommGame, string platformName)
        {
            game.Title = NormalizeGameTitle(rommGame.Name);

            game.Platform = platformName;

            var isFolderGame = rommGame.Files.Count > 1;

            SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
            SetCustomField(game, GameCustomFields.FileName, (rommGame.FsName + (isFolderGame ? ".zip" : "")) ?? "");
            SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());
        }

        private void AddInstallUninstallIfMissing(IGame game, int rommId)
        {
            var hasInstallApp = game.GetAllAdditionalApplications()
                .Any(a => a.Name == "Install (RomM)");

            if (!hasInstallApp)
            {
                var installApp = game.AddNewAdditionalApplication();
                installApp.Name = "Install (RomM)";
                installApp.ApplicationPath = ".\\Plugins\\RomM LaunchBox Integration\\RommPlugin.CLI.exe";
                installApp.CommandLine = $"install {rommId}";
                installApp.AutoRunAfter = false;
            }

            var hasUninstallApp = game.GetAllAdditionalApplications()
                .Any(a => a.Name == "Uninstall (RomM)");

            if (!hasUninstallApp)
            {
                var uninstallApp = game.AddNewAdditionalApplication();
                uninstallApp.Name = "Uninstall (RomM)";
                uninstallApp.ApplicationPath = ".\\Plugins\\RomM LaunchBox Integration\\RommPlugin.CLI.exe";
                uninstallApp.CommandLine = $"uninstall {rommId}";
                uninstallApp.AutoRunAfter = false;
            }
        }

        private string parseCategory(string category)
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

        private void CheckAndSavePlatforms(List<RommPlatform> rommPlatforms, List<IPlatform> launchboxPlatforms)
        {
            var settings = RommPluginStorage.Load();
            var manager = PluginHelper.DataManager;
            var serverIds = new HashSet<int>(rommPlatforms.Select(p => p.Id));

            foreach (var rommPlatform in rommPlatforms)
            {
                var name = !string.IsNullOrWhiteSpace(rommPlatform.CustomName)
                    ? rommPlatform.CustomName
                    : rommPlatform.Name;

                var platformName = $"RomM | {name}";

                var existing = settings.CurrentPlatforms
                    .FirstOrDefault(p => p.Id == rommPlatform.Id);

                if (existing == null)
                {
                    settings.CurrentPlatforms.Add(new RommCurrentPlatform
                    {
                        Id = rommPlatform.Id,
                        Name = platformName
                    });
                }
                else
                {
                    existing.Name = platformName;
                }
            }

            var removedPlatforms = settings.CurrentPlatforms
                .Where(p => !serverIds.Contains(p.Id))
                .ToList();

            foreach (var removed in removedPlatforms)
            {
                var platform = launchboxPlatforms
                    .FirstOrDefault(p => p.Name == removed.Name);

                if (platform != null)
                {
                    manager.TryRemovePlatform(platform);
                }

                settings.CurrentPlatforms.Remove(removed);
            }

            RommPluginStorage.Save(settings);
            manager.Save();
        }

        public async Task ProcessSyncEvents()
        {
            await ProgressRunner.RunAsync(
                "Processing installations events",
                async progress =>
                {
                    var flagPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..",
                        "Plugins",
                        "RomM LaunchBox Integration",
                        "romm.sync"
                    );

                    if (!File.Exists(flagPath))
                    {
                        MessageBox.Show("Romm do not have any pending install");
                        return;
                    }

                    var file = JsonConvert.DeserializeObject<RommSyncFile>(File.ReadAllText(flagPath));

                    if (file?.Events == null || file.Events.Count == 0)
                    {
                        MessageBox.Show("RomM do not have any pending install");
                        return;
                    }

                    var settings = RommPluginStorage.Load();
                    var dataManager = PluginHelper.DataManager;

                    var rommGamesOnly = dataManager.GetAllGames()
                        .Where(g => g.Platform != null && g.Platform.StartsWith("RomM | "))
                        .ToList();

                    var gamesById = new Dictionary<int, IGame>();

                    foreach (var game in rommGamesOnly)
                    {
                        if (TryGetRommId(game, out var id))
                        {
                            gamesById[id] = game;
                        }
                    }

                    var totalEvents = file.Events.Count;
                    var completedEvents = 0;

                    await Task.Run(() =>
                    {
                        foreach (var evt in file.Events.ToList())
                        {
                            progress.SetStatus($"Processing: {completedEvents} of {totalEvents}");

                            var installed = false;

                            if (!gamesById.TryGetValue(evt.RommGameId, out var game))
                            {
                                continue;
                            }

                            if (evt.Action == "install")
                            {
                                var fields = game.GetAllCustomFields().ToDictionary(f => f.Name, f => f.Value);

                                fields.TryGetValue(GameCustomFields.RemotePath, out var remotePath);
                                fields.TryGetValue(GameCustomFields.FileName, out var fileName);
                                fields.TryGetValue(GameCustomFields.IsFolderGame, out var folderValue);

                                var isFolderGame = folderValue == bool.TrueString;

                                var localFile = Path.Combine(
                                    settings.RomsPath,
                                    "romm",
                                    remotePath.Replace("/", "\\"),
                                    fileName
                                );

                                if (isFolderGame)
                                {
                                    var zipPath = localFile;
                                    var extractDir = Path.Combine(
                                        Path.GetDirectoryName(zipPath),
                                        Path.GetFileNameWithoutExtension(zipPath)
                                    );

                                    UnzipAndDelete(zipPath, extractDir);

                                    var jsonPath = Path.Combine(extractDir, "_launchbox.json");

                                    if (File.Exists(jsonPath))
                                    {
                                        ConfigureLaunchBoxGame(game, extractDir, jsonPath);
                                    }

                                    localFile = extractDir;
                                    installed = Directory.Exists(localFile);
                                }
                                else
                                {
                                    installed = File.Exists(localFile);
                                    game.ApplicationPath = installed ? localFile : null;
                                }
                            }
                            else if (evt.Action == "uninstall")
                            {
                                ClearGameAdditionalApplications(game);
                                game.ApplicationPath = null;
                                installed = false;
                            }

                            game.Installed = installed;

                            file.Events.Remove(evt);

                            completedEvents++;
                        }
                    });

                    dataManager.Save();

                    if (file.Events.Count == 0)
                    {
                        File.Delete(flagPath);
                    }
                    else
                    {
                        File.WriteAllText(
                            flagPath,
                            JsonConvert.SerializeObject(file, Formatting.Indented)
                        );
                    }
                }
            );
        }

        private void ConfigureLaunchBoxGame(IGame game, string baseFolder, string jsonPath)
        {
            var config = JsonConvert.DeserializeObject<LaunchBoxFolderGameConfig>(File.ReadAllText(jsonPath));

            if (config == null)
            {
                MessageBox.Show("RomM error while get game folder configuration");
                return;
            }

            ClearGameAdditionalApplications(game);

            if (!string.IsNullOrWhiteSpace(config.DefaultFileName))
            {
                game.ApplicationPath = Path.Combine(baseFolder, config.DefaultFileName);
            }

            if (config.AdditionalApplications != null)
            {
                foreach (var app in config.AdditionalApplications)
                {
                    var add = game.AddNewAdditionalApplication();
                    add.Name = app.Name;
                    add.ApplicationPath = Path.Combine(baseFolder, app.Path);
                    add.CommandLine = app.CommandLine;
                }
            }

            if (config.PreLoaders != null)
            {
                foreach (var loader in config.PreLoaders)
                {
                    var add = game.AddNewAdditionalApplication();
                    add.Name = loader.Name;
                    add.ApplicationPath = Path.Combine(baseFolder, loader.Path);
                    add.CommandLine = loader.CommandLine;
                    add.AutoRunBefore = true;
                    add.WaitForExit = loader.WaitForExit ?? false;
                }
            }

            if (config.PosLoaders != null)
            {
                foreach (var loader in config.PosLoaders)
                {
                    var add = game.AddNewAdditionalApplication();
                    add.Name = loader.Name;
                    add.ApplicationPath = Path.Combine(baseFolder, loader.Path);
                    add.CommandLine = loader.CommandLine;
                    add.AutoRunAfter = true;
                }
            }

            if (config.HasDLC == true)
            {
                var dlcFolder = Path.Combine(baseFolder, "_DLCs");

                if (Directory.Exists(dlcFolder))
                {
                    var files = Directory.GetFiles(dlcFolder);

                    int index = 1;
                    foreach (var file in files)
                    {
                        var add = game.AddNewAdditionalApplication();
                        add.Name = $"DLC {index}";
                        add.ApplicationPath = file;
                        index++;
                    }
                }
            }
        }

        private void ClearGameAdditionalApplications(IGame game)
        {
            var applications = game.GetAllAdditionalApplications()
                .Where(a => !a.Name.Contains("(RomM)"))
                .ToList();

            foreach (var app in applications)
            {
                game.TryRemoveAdditionalApplication(app);
            }
        }

        private void UnzipAndDelete(string zipPath, string extractDir)
        {
            var rootFolder = Path.GetFileNameWithoutExtension(zipPath);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
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
                        continue;
                    }

                    var relativePath = Path.Combine(parts);

                    var destinationPath = Path.Combine(extractDir, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    entry.ExtractToFile(destinationPath, true);
                }
            }

            File.Delete(zipPath);
        }

        public async Task RemoveAllGamesServerMetadata(string username, string password)
        {
            await ProgressRunner.RunAsync(
                "Reset Metadata in RomM server...",
                async progress =>
                {
                    _api.SetBasicAuthentication(username, password);

                    var rommPlatforms = await _api.GetPlatformsAsync();

                    if (rommPlatforms == null || rommPlatforms.Count == 0)
                    {
                        return;
                    }

                    var platformsCompleted = 0;
                    var platformsTotal = rommPlatforms.Count;

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        var rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);

                        if (rommGames == null)
                        {
                            continue;
                        }

                        progress.SetTitle($"RomM: Delete all metadata");

                        var completedGames = 0;
                        var totalGames = rommGames.Count;

                        foreach (var rommGame in rommGames)
                        {
                            progress.SetStatus($"Platform: {platformsCompleted} of {platformsTotal} | Games for this platform: {completedGames} of {totalGames}");

                            await _api.RemoveGameMetadataById(rommGame.Id);

                            completedGames++;
                        }

                        platformsCompleted++;
                    }
                }
            );
        }

        public async Task UpdateServerMetadata(string username, string password)
        {
            await ProgressRunner.RunAsync(
                "Reset Metadata in RomM server...",
                async progress =>
                {
                    _api.SetBasicAuthentication(username, password);

                    var dataManager = PluginHelper.DataManager;
                    var settings = RommPluginStorage.Load();

                    if (settings.CurrentPlatforms == null || settings.CurrentPlatforms.Count == 0)
                    {
                        MessageBox.Show("No RomM platforms available.");
                        return;
                    }

                    var list = settings.CurrentPlatforms
                        .Select(p => new PlatformSelection
                        {
                            Id = p.Id,
                            Name = p.Name,
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
                        MessageBox.Show("No RomM games available.");
                        return;
                    }

                    var completedGames = 0;
                    var gamesTotal = rommGamesOnly.Count;

                    progress.SetTitle($"RomM: Update all metadata");

                    foreach (var game in rommGamesOnly)
                    {
                        TryGetRommId(game, out int rommId);
                        var serverGame = await _api.GetGameByIdAsync(rommId);

                        if (serverGame == null)
                        {
                            continue;
                        }

                        progress.SetStatus($"Games: {completedGames} of {gamesTotal}");

                        var request = new RommUpdateGameRequest
                        {
                            Name = game.Title,
                            FsName = serverGame.FsName,
                            Summary = game.Notes,
                            LaunchboxId = game.LaunchBoxDbId,
                            RawLaunchboxMetadata = BuildLaunchboxMetadata(game),
                            ArtworkPath = GetCoverImagePath(game)
                        };

                        await _api.UpdateGameById(rommId, request);

                        completedGames++;
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

        private LaunchBoxMetadataModel BuildLaunchboxMetadata(IGame game)
        {
            var metadata = new LaunchBoxMetadataModel
            {
                FirstReleaseDate = game.ReleaseDate != null
                    ? new DateTimeOffset(game.ReleaseDate.Value).ToUnixTimeSeconds()
                    : (long?)null,
                MaxPlayers = game.MaxPlayers ?? 1,
                ReleaseType = string.IsNullOrEmpty(game.ReleaseType) ? "Released" : game.ReleaseType,
                Cooperative = game.PlayMode == "Cooperative",
                YoutubeVideoId = ExtractYoutubeId(game.VideoUrl),
                CommunityRating = game.CommunityStarRating,
                CommunityRatingCount = game.CommunityStarRatingTotalVotes,
                WikipediaUrl = game.WikipediaUrl,
                Esrb = game.Rating,
                Genres = game.Genres?.ToList() ?? new List<string>(),
                Companies = game.Developers?.ToList() ?? new List<string>(),
                Images = new List<LaunchBoxImage>()
            };

            return metadata;
        }

        private string ExtractYoutubeId(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }

            var uri = new Uri(url);
            var query = uri.Query;

            var match = System.Text.RegularExpressions.Regex.Match(query, @"v=([^&]+)");
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}