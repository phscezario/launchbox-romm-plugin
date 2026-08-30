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
using RommPlugin.Core.Logging;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using RommPlugin.UI.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public class RommProcessInstallUninstallService : IRommProcessInstallUninstallService
    {
        public async Task ProcessInstallUninstallEvents(bool showEmptyMessage = true)
        {
            await ProgressRunner.RunAsync(
                LocaleManager.Get("progress.processing"),
                async progress =>
                {
                    var stateFilePath = RommPaths.DownloadStateFile;

                    if (!File.Exists(stateFilePath))
                    {
                        if (showEmptyMessage)
                        {
                            MessageBox.Show(LocaleManager.Get("progress.no_pending"));
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
                        .Where(i => i.Status == DownloadStatus.WaitingInstall)
                        .ToList();

                    if (waitingItems == null || waitingItems.Count == 0)
                    {
                        if (showEmptyMessage)
                        {
                            MessageBox.Show(LocaleManager.Get("progress.no_pending"));
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

                    foreach (var item in waitingItems)
                    {
                        progress.SetStatus($"Processing: {completedItems} of {totalItems}");

                        try
                        {
                            if (!gamesById.TryGetValue(item.GameId, out var game))
                            {
                                item.Status = DownloadStatus.Failed;
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
                                item.Status = DownloadStatus.Failed;
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
                                completedItems++;
                                continue;
                            }

                            var extractDir = Path.Combine(
                                Path.GetDirectoryName(zipPath),
                                Path.GetFileNameWithoutExtension(zipPath)
                            );

                            if (isFolderGame)
                            {
                                UnzipAndDelete(zipPath, extractDir);

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

                            item.Status = DownloadStatus.Installed;
                            item.CompletedAt = DateTime.UtcNow;
                            completedItems++;
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogException(ex);
                            item.Status = DownloadStatus.Failed;
                            item.Error = ex.Message;
                        }
                    }

                    var tempPath = Path.Combine(Path.GetDirectoryName(stateFilePath), $"download-state.{Guid.NewGuid():N}.tmp");
                    try
                    {
                        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                        File.WriteAllText(tempPath, json);
                        File.Copy(tempPath, stateFilePath, true);
                    }
                    catch (Exception ex)
                    {
                        RommLogger.LogError($"[RommPlugin] Failed to save download-state.json: {ex.Message}");
                    }
                    finally
                    {
                        try { File.Delete(tempPath); } catch { }
                    }

                    dataManager.Save();

                    RommLogger.Log("Pending installs processed successfully");

                    if (showEmptyMessage)
                    {
                        MessageBox.Show("RomM finish all pending install");
                    }
                }
            );
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

            var existingPaths = new HashSet<string>(
                game.GetAllAdditionalApplications()
                    .Where(a => !string.IsNullOrEmpty(a.ApplicationPath))
                    .Select(a => a.ApplicationPath),
                StringComparer.OrdinalIgnoreCase);

            if (config.AdditionalApplications != null)
            {
                foreach (var app in config.AdditionalApplications)
                {
                    var resolvedPath = RommGameHelpers.ResolvePath(baseFolder, app.Path, false);
                    if (existingPaths.Contains(resolvedPath)) continue;

                    var add = game.AddNewAdditionalApplication();
                    add.Name = app.Name;
                    add.ApplicationPath = resolvedPath;
                    add.CommandLine = app.CommandLine;
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
                    add.Name = loader.Name;
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
                    add.Name = loader.Name;
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

                    int index = 1;
                    foreach (var file in files)
                    {
                        if (existingPaths.Contains(file)) continue;

                        var add = game.AddNewAdditionalApplication();
                        add.Name = $"DLC {index}";
                        add.ApplicationPath = file;
                        existingPaths.Add(file);
                        index++;
                    }
                }
            }
        }



        private void UnzipAndDelete(string zipPath, string extractDir)
        {
            var rootFolder = Path.GetFileNameWithoutExtension(zipPath);

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
                    var destinationPath = Path.Combine(extractDir, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    entry.ExtractToFile(destinationPath, true);
                }
            }

            File.Delete(zipPath);
        }

        private void UnzipAndFlatten(string zipPath)
        {
            if (!File.Exists(zipPath) || !Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var tempExtract = Path.Combine(
                Path.GetDirectoryName(zipPath),
                $"_temp_{Guid.NewGuid():N}"
            );

            if (Directory.Exists(tempExtract))
            {
                Directory.Delete(tempExtract, true);
            }

            Directory.CreateDirectory(tempExtract);

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
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Copy(file, dest, true);
                    }

                    File.Delete(zipPath);
                }
                else if (allFiles.Length == 1)
                {
                    var innerFile = allFiles[0];
                    var innerExt = Path.GetExtension(innerFile).ToLowerInvariant();

                    if (innerExt == ".zip")
                    {
                        File.Copy(innerFile, zipPath, true);
                    }
                    else
                    {
                        var targetPath = Path.Combine(
                            Path.GetDirectoryName(zipPath),
                            Path.GetFileName(innerFile)
                        );
                        File.Copy(innerFile, targetPath, true);
                        File.Delete(zipPath);
                    }
                }
            }
            finally
            {
                try { Directory.Delete(tempExtract, true); } catch { }
            }
        }
    }
}