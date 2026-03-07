using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public class RommProcessInstallUninstallService
    {
        public async Task ProcessInstallUninstallEvents()
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

        private bool TryGetRommId(IGame game, out int rommId)
        {
            rommId = 0;

            var value = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out rommId);
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
    }
}