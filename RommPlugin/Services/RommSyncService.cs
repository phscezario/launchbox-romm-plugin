using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
                                UpdateGame(existingGame, rommGame, platform.Name, !settings.KeepLocalData);
                                hasChanges = true;
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

        public object RoomImageService { get; private set; }

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

        private void UpdateGame(IGame game, RommGame rommGame, string platformName, bool overwriteLocalData)
        {
            if (overwriteLocalData)
            {
                game.Title = NormalizeGameTitle(rommGame.Name);

                game.Platform = platformName;
            }

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

                        var artworkPath = GetCoverImagePath(game);
                        var originalArtwork = artworkPath;

                        if (!string.IsNullOrEmpty(artworkPath) && File.Exists(artworkPath))
                        {
                            artworkPath = RommImageService.EnsureRgbJpeg(artworkPath);
                        }

                        var request = new RommUpdateGameRequest
                        {
                            Name = game.Title,
                            FsName = serverGame.FsName,
                            Summary = game.Notes,
                            LaunchboxId = game.LaunchBoxDbId,
                            RawLaunchboxMetadata = LaunchboxMetadaService.BuildLaunchboxMetadata(game),
                            ArtworkPath = artworkPath
                        };

                        await _api.UpdateGameById(rommId, request);

                        if (!string.IsNullOrEmpty(artworkPath) && artworkPath != originalArtwork)
                        {
                            File.Delete(artworkPath);
                        }

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
    }
}