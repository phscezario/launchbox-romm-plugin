using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
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
    public class RommProcessInstallUninstallService
    {
        public async Task ProcessInstallUninstallEvents(bool showEmptyMessage = true)
        {
            await ProgressRunner.RunAsync(
                LocaleManager.Get("progress.processing"),
                async progress =>
                {
                    var stateFilePath = RommPaths.DownloadStateFile;

                    RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: stateFilePath={stateFilePath}, exists={File.Exists(stateFilePath)}");

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

                    RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: {waitingItems?.Count ?? 0} WaitingInstall items");

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
                        .Where(g => g.Platform != null && g.Platform.StartsWith("RomM | "))
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
                        RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: processing {completedItems+1}/{totalItems}, gameId={item.GameId}, fsName={item.FsName}");

                        try
                        {
                            if (!gamesById.TryGetValue(item.GameId, out var game))
                            {
                                RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: game {item.GameId} not found in LaunchBox");
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
                            RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: remotePath={remotePath}, fileName={fileName}, isFolderGame={isFolderGame}");

                            var localFile = Path.Combine(
                                settings.RomsPath,
                                "romm",
                                remotePath.Replace("/", "\\"),
                                fileName
                            );

                            RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: localFile={localFile}");

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

                            RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: zipPath={zipPath}, exists={File.Exists(zipPath)}");

                            if (!isFolderGame && File.Exists(zipPath) && Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var archive = ZipFile.OpenRead(zipPath))
                                {
                                    var entries = archive.Entries.Where(e => !string.IsNullOrWhiteSpace(e.Name)).ToList();
                                    var hasSubdirs = entries.Any(e => e.FullName.Contains("/"));
                                    RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: zip check: entries={entries.Count}, hasSubdirs={hasSubdirs}");
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
                                        RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: auto-detected folder game, updated FileName: {fileName} → {newFileName}");
                                    }
                                }
                            }

                            if (!File.Exists(zipPath) && !isFolderGame)
                            {
                                RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: file not found for {item.GameId}, zipPath={zipPath}, leaving as WaitingInstall for retry");
                                completedItems++;
                                continue;
                            }

                            var extractDir = Path.Combine(
                                Path.GetDirectoryName(zipPath),
                                Path.GetFileNameWithoutExtension(zipPath)
                            );

                            RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: extractDir={extractDir}");

                            if (isFolderGame)
                            {
                                RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: calling UnzipAndDelete");
                                UnzipAndDelete(zipPath, extractDir);

                                var jsonPath = Path.Combine(extractDir, "_launchbox.json");

                                if (File.Exists(jsonPath))
                                {
                                    RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: _launchbox.json found, configuring game");
                                    ConfigureLaunchBoxGame(game, extractDir, jsonPath);
                                }

                                localFile = extractDir;
                            }
                            else
                            {
                                RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: calling UnzipAndFlatten");
                                UnzipAndFlatten(zipPath);

                                if (!File.Exists(localFile) && Directory.Exists(extractDir))
                                {
                                    localFile = extractDir;
                                    RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: zip deleted by UnzipAndFlatten, using extractDir={extractDir}");

                                    var jsonPath = Path.Combine(extractDir, "_launchbox.json");
                                    if (File.Exists(jsonPath))
                                    {
                                        RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: _launchbox.json found in extractDir, configuring game");
                                        ConfigureLaunchBoxGame(game, extractDir, jsonPath);
                                    }
                                }
                                else if (!File.Exists(localFile) && !Directory.Exists(localFile))
                                {
                                    var zipVariant = localFile + ".zip";
                                    if (File.Exists(zipVariant))
                                    {
                                        localFile = zipVariant;
                                        RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: fallback .zip variant found={localFile}");
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
                                                RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: fallback extracted file found={localFile}");
                                            }
                                        }
                                    }
                                }

                                game.ApplicationPath = File.Exists(localFile) ? localFile : (Directory.Exists(localFile) ? localFile : null);
                            }

                            game.Installed = isFolderGame ? Directory.Exists(localFile) : File.Exists(localFile);
                            RommLogger.Log($"[DIAG] ProcessInstallUninstallEvents: game.Installed={game.Installed}, ApplicationPath={game.ApplicationPath}");

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

            ClearGameAdditionalApplications(game, baseFolder);

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
                    var resolvedPath = ResolvePath(baseFolder, app.Path, false);
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
                    var resolvedPath = ResolvePath(baseFolder, loader.Path, loader.FromLaunchBoxRoot ?? false);
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
                    var resolvedPath = ResolvePath(baseFolder, loader.Path, loader.FromLaunchBoxRoot ?? false);
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

        private string ResolvePath(string baseFolder, string path, bool fromLaunchBoxRoot)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            if (fromLaunchBoxRoot)
            {
                return path;
            }

            return Path.GetFullPath(Path.Combine(baseFolder, path));
        }

        private void ClearGameAdditionalApplications(IGame game, string installedPath)
        {
            if (string.IsNullOrEmpty(installedPath)) return;

            var jsonPath = Path.Combine(installedPath, "_launchbox.json");
            if (!File.Exists(jsonPath)) return;

            var config = JsonConvert.DeserializeObject<LaunchBoxFolderGameConfig>(File.ReadAllText(jsonPath));
            if (config == null) return;

            var jsonPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (config.AdditionalApplications != null)
                foreach (var app in config.AdditionalApplications)
                    if (!string.IsNullOrEmpty(app.Path))
                        jsonPaths.Add(ResolvePath(installedPath, app.Path, false));

            if (config.PreLoaders != null)
                foreach (var loader in config.PreLoaders)
                    if (!string.IsNullOrEmpty(loader.Path))
                        jsonPaths.Add(ResolvePath(installedPath, loader.Path, loader.FromLaunchBoxRoot ?? false));

            if (config.PosLoaders != null)
                foreach (var loader in config.PosLoaders)
                    if (!string.IsNullOrEmpty(loader.Path))
                        jsonPaths.Add(ResolvePath(installedPath, loader.Path, loader.FromLaunchBoxRoot ?? false));

            var apps = game.GetAllAdditionalApplications().ToList();
            foreach (var app in apps)
            {
                if (!string.IsNullOrEmpty(app.ApplicationPath) && jsonPaths.Contains(app.ApplicationPath))
                {
                    game.TryRemoveAdditionalApplication(app);
                }
            }
        }

        private void UnzipAndDelete(string zipPath, string extractDir)
        {
            var rootFolder = Path.GetFileNameWithoutExtension(zipPath);
            RommLogger.Log($"[DIAG] UnzipAndDelete: zipPath={zipPath}, extractDir={extractDir}, rootFolder={rootFolder}");

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var entryList = archive.Entries.ToList();
                RommLogger.Log($"[DIAG] UnzipAndDelete: {entryList.Count} entries in archive");

                foreach (var entry in entryList)
                {
                    RommLogger.Log($"[DIAG] UnzipAndDelete: entry FullName={entry.FullName}, Name={entry.Name}");

                    if (string.IsNullOrWhiteSpace(entry.Name))
                    {
                        RommLogger.Log($"[DIAG] UnzipAndDelete: skipping empty entry");
                        continue;
                    }

                    var parts = entry.FullName
                        .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                        .SkipWhile(p => p != rootFolder)
                        .Skip(1)
                        .ToArray();

                    if (parts.Length == 0)
                    {
                        RommLogger.Log($"[DIAG] UnzipAndDelete: rootFolder '{rootFolder}' not found in path, using full path as fallback");
                        parts = entry.FullName
                            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                            .ToArray();
                    }
                    else
                    {
                        RommLogger.Log($"[DIAG] UnzipAndDelete: stripped to [{string.Join(", ", parts)}]");
                    }

                    var relativePath = Path.Combine(parts);
                    var destinationPath = Path.Combine(extractDir, relativePath);

                    RommLogger.Log($"[DIAG] UnzipAndDelete: extracting to {destinationPath}");

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    entry.ExtractToFile(destinationPath, true);
                }
            }

            File.Delete(zipPath);
            RommLogger.Log($"[DIAG] UnzipAndDelete: completed, deleted zip");
        }

        private void UnzipAndFlatten(string zipPath)
        {
            RommLogger.Log($"[DIAG] UnzipAndFlatten: zipPath={zipPath}");

            if (!File.Exists(zipPath) || !Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                RommLogger.Log($"[DIAG] UnzipAndFlatten: invalid zip, returning");
                return;
            }

            var tempExtract = Path.Combine(
                Path.GetDirectoryName(zipPath),
                $"_temp_{Guid.NewGuid():N}"
            );

            RommLogger.Log($"[DIAG] UnzipAndFlatten: tempExtract={tempExtract}");

            if (Directory.Exists(tempExtract))
            {
                Directory.Delete(tempExtract, true);
            }

            Directory.CreateDirectory(tempExtract);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempExtract);

                var allFiles = Directory.GetFiles(tempExtract, "*", SearchOption.AllDirectories);
                RommLogger.Log($"[DIAG] UnzipAndFlatten: extracted {allFiles.Length} files to temp");

                if (allFiles.Length > 1)
                {
                    RommLogger.Log($"[DIAG] UnzipAndFlatten: multi-file zip, treating as folder game");
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
                    RommLogger.Log($"[DIAG] UnzipAndFlatten: folder game extracted, deleted wrapper zip");
                }
                else if (allFiles.Length == 1)
                {
                    var innerFile = allFiles[0];
                    var innerExt = Path.GetExtension(innerFile).ToLowerInvariant();
                    RommLogger.Log($"[DIAG] UnzipAndFlatten: single file={Path.GetFileName(innerFile)}, ext={innerExt}");

                    if (innerExt == ".zip")
                    {
                        RommLogger.Log($"[DIAG] UnzipAndFlatten: inner zip, copying to {zipPath}");
                        File.Copy(innerFile, zipPath, true);
                    }
                    else
                    {
                        var targetPath = Path.Combine(
                            Path.GetDirectoryName(zipPath),
                            Path.GetFileName(innerFile)
                        );
                        RommLogger.Log($"[DIAG] UnzipAndFlatten: extracting to {targetPath}");
                        File.Copy(innerFile, targetPath, true);
                        File.Delete(zipPath);
                    }
                }
                else
                {
                    RommLogger.Log($"[DIAG] UnzipAndFlatten: no files found in zip");
                }

                RommLogger.Log($"[DIAG] UnzipAndFlatten: completed");
            }
            finally
            {
                try { Directory.Delete(tempExtract, true); } catch { }
            }
        }
    }
}