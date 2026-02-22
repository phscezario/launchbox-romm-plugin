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

        public async Task SyncAsync(string username, string password)
        {
            await ProgressRunner.RunAsync(
                "Starting sync from RomM...",
                async progress =>
                {
                    await _api.LoginAsync(username, password);

                    var dataManager = PluginHelper.DataManager;

                    var platforms = dataManager.GetAllPlatforms().ToList();
                    var games = dataManager.GetAllGames().ToList();
                    var platformCategories = dataManager.GetAllPlatformCategories().ToList();

                    var rommPlatforms = await _api.GetPlatformsAsync();
                    int platformCompleted = 0;
                    int platformTotal = rommPlatforms.Count;

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        try
                        {
                            var parsedCategory = parseCategory(rommPlatform.Category);
                            var rommCategoryName = $"RomM | {parsedCategory}";

                            var rommCategory = platformCategories.FirstOrDefault(p => p.Name == rommCategoryName);

                            if (rommCategory == null)
                            {
                                rommCategory = dataManager.AddNewPlatformCategory(rommCategoryName);
                                platformCategories.Add(rommCategory);
                            }

                            var name = rommPlatform.CustomName != "" && rommPlatform.CustomName != null
                                ? rommPlatform.CustomName
                                : rommPlatform.Name;

                            var platformName = $"RomM | {name}";
                            var platform = platforms.FirstOrDefault(p => p.Name == platformName);

                            if (platform == null)
                            {
                                platform = dataManager.AddNewPlatform(platformName);
                                platform.Category = rommCategoryName;
                                platforms.Add(platform);
                            }

                            List<RommGame> rommGames = null;

                            try
                            {
                                rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);
                            }
                            catch (Exception error)
                            {
                                MessageBox.Show(error.ToString(), "Romm Plugin");
                                continue;
                            }

                            int completedGames = 0;
                            int totalGames = rommGames.Count;

                            progress.SetTitle($"RomM: Installing games from {platform.Name}");

                            foreach (var rommGame in rommGames)
                            {
                                try
                                {
                                    progress.SetStatus($"Platform: {platformCompleted} of {platformTotal} | Games for this platform: {completedGames} of {totalGames}");

                                    var exists = GameExistsByRommId(games, rommGame.Id);

                                    if (!exists)
                                    {
                                        var game = dataManager.AddNewGame(rommGame.Name);
                                        game.Platform = platform.Name;

                                        var isFolderGame = rommGame.Files.Count > 1;

                                        SetCustomField(game, GameCustomFields.GameId, rommGame.Id.ToString());
                                        SetCustomField(game, GameCustomFields.PlatformId, rommPlatform.Id.ToString());
                                        SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
                                        SetCustomField(game, GameCustomFields.FileName, (rommGame.FsName + (isFolderGame ? ".zip" : "")) ?? "");
                                        SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());

                                        bool hasInstallApp = game.GetAllAdditionalApplications().Any(a => a.Name == "Install (RomM)");

                                        if (!hasInstallApp)
                                        {
                                            var installApp = game.AddNewAdditionalApplication();
                                            installApp.Name = "Install (RomM)";
                                            installApp.ApplicationPath = ".\\Plugins\\RomM LaunchBox Integration\\RommPlugin.CLI.exe";
                                            installApp.CommandLine = $"install {rommGame.Id.ToString()}";
                                            installApp.AutoRunAfter = false;
                                        }

                                        bool hasUninstallApp = game.GetAllAdditionalApplications().Any(a => a.Name == "Uninstall (RomM)");

                                        if (!hasUninstallApp)
                                        {
                                            var uninstallApp = game.AddNewAdditionalApplication();
                                            uninstallApp.Name = "Uninstall (RomM)";
                                            uninstallApp.ApplicationPath = ".\\Plugins\\RomM LaunchBox Integration\\RommPlugin.CLI.exe";
                                            uninstallApp.CommandLine = $"uninstall {rommGame.Id.ToString()}";
                                            uninstallApp.AutoRunAfter = false;
                                        }

                                        game.Installed = game.Installed != null ? game.Installed : false;
                                        games.Add(game);
                                    }

                                }
                                catch (Exception error)
                                {
                                    MessageBox.Show(error.ToString(), "Romm Plugin");
                                    continue;
                                }
                                finally
                                {
                                    completedGames++;

                                }
                            }
                        }
                        catch (Exception error)
                        {
                            MessageBox.Show(error.ToString(), "Romm Plugin");
                        }
                        finally
                        {
                            dataManager.Save();
                            platformCompleted++;
                        }
                    }
                }
            );
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

        void SetCustomField(IGame game, string name, string value, bool overwrite = true)
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

        bool GameExistsByRommId(List<IGame> games, int rommGameId)
        {
            return games.Any(g => g.GetAllCustomFields().Any(f => f.Name == GameCustomFields.GameId && f.Value == rommGameId.ToString()));
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
                        MessageBox.Show("Romm do not have any pending install");
                        return;
                    }

                    var settings = RommPluginStorage.Load();
                    var dataManager = PluginHelper.DataManager;
                    var games = dataManager.GetAllGames().ToList();

                    var totalEvents = file.Events.Count;
                    var completedEvents = 0;

                    await Task.Run(() =>
                    {
                        foreach (var evt in file.Events.ToList())
                        {
                            progress.SetStatus($"Processing: {completedEvents} of {totalEvents}");

                            var game = games.FirstOrDefault(g =>
                                g.GetAllCustomFields()
                                 .Any(f => f.Name == GameCustomFields.GameId &&
                                           f.Value == evt.RommGameId.ToString())
                            );

                            var installed = false;

                            if (game == null)
                            {
                                continue;
                            }

                            if (evt.Action == "install")
                            {
                                var remotePath = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.RemotePath)?.Value;

                                var fileName = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.FileName)?.Value;

                                var isFolderGame = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.IsFolderGame)?.Value == true.ToString();

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

                    MessageBox.Show("Romm finish all pending install");
                }
            );
        }

        private void ConfigureLaunchBoxGame(IGame game, string baseFolder, string jsonPath)
        {
            var config = JsonConvert.DeserializeObject<LaunchBoxFolderGameConfig>(File.ReadAllText(jsonPath));

            if (config == null)
            {
                MessageBox.Show("Romm error while get game folder configuration");
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
    }
}