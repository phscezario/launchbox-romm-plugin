using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using RommPlugin.Services;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommInstallMenuItem : RommMenuItem, IGameMenuItemPlugin
    {
        public override string Caption => LocaleManager.Get("context.install");

        public bool SupportsMultipleGames => false;

        public bool GetIsValidForGame(IGame selectedGame)
        {
            return RommGameHelpers.TryGetRommId(selectedGame, out _)
                && selectedGame.Installed != true;
        }

        public override void OnSelected()
        {
        }

        public void OnSelected(IGame selectedGame)
        {
            if (!RommGameHelpers.TryGetRommId(selectedGame, out var rommId))
            {
                return;
            }

            var settings = RommPluginStorage.Load();
            if (string.IsNullOrWhiteSpace(settings.RomsPath))
            {
                using (var form = new ConfirmForm(LocaleManager.Get("error.settings_not_configured")))
                    form.ShowDialog();
                return;
            }

            var fields = selectedGame.GetAllCustomFields().GroupBy(f => f.Name).ToDictionary(g => g.Key, g => g.Last().Value);

            fields.TryGetValue(GameCustomFields.RemotePath, out var remotePath);
            fields.TryGetValue(GameCustomFields.FileName, out var fileName);

            if (string.IsNullOrEmpty(remotePath) || string.IsNullOrEmpty(fileName))
            {
                using (var form = new ConfirmForm(
                    string.Format(LocaleManager.Get("error.game_not_found"), rommId)))
                    form.ShowDialog();
                return;
            }

            var pluginDir = RommPaths.PluginFolder;

            // Validação 1: já instalado via installed-games.json
            var installedService = ServiceLocator.GetService<IInstalledGamesService>();
            if (installedService.IsInstalled(rommId))
            {
                using (var form = new ConfirmForm(LocaleManager.Get("install.already_installed")))
                    form.ShowDialog();
                return;
            }

            // Validação 2: já na fila de download (download-queue.json)
            var queueFilePath = Path.Combine(pluginDir, RommConstants.DownloadQueueFile);
            if (File.Exists(queueFilePath))
            {
                try
                {
                    var existingQueue = JsonConvert.DeserializeObject<List<QueueAction>>(File.ReadAllText(queueFilePath));
                    if (existingQueue != null && existingQueue.Any(a => a.GameId == rommId && a.Action == "add"))
                    {
                        using (var form = new ConfirmForm(LocaleManager.Get("install.already_queued")))
                            form.ShowDialog();
                        return;
                    }
                }
                catch
                {
                }
            }

            // Validação 3: já em download ativo (download-state.json)
            var stateFilePath = Path.Combine(pluginDir, RommConstants.DownloadStateFile);
            if (File.Exists(stateFilePath))
            {
                try
                {
                    var state = JsonConvert.DeserializeObject<DownloadState>(File.ReadAllText(stateFilePath));
                    if (state?.Items != null)
                    {
                        var activeItem = state.Items.FirstOrDefault(i =>
                            i.GameId == rommId &&
                            (i.Status == DownloadStatus.Downloading ||
                             i.Status == DownloadStatus.Pending ||
                             i.Status == DownloadStatus.WaitingInstall));
                        if (activeItem != null)
                        {
                            using (var form = new ConfirmForm(LocaleManager.Get("install.already_queued")))
                                form.ShowDialog();
                            return;
                        }
                    }
                }
                catch
                {
                }
            }

            // Validação 4: arquivo local já existe no disco
            var localFile = Path.Combine(settings.RomsPath, RommConstants.RomsSubfolder, remotePath.Replace("/", "\\"), fileName);

            if (File.Exists(localFile) || Directory.Exists(localFile))
            {
                using (var form = new ConfirmForm(
                    string.Format(LocaleManager.Get("install.already_exists"), selectedGame.Title)))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                }
            }

            // Tudo OK: adicionar à fila de download

            var queueActions = new List<QueueAction>();
            if (File.Exists(queueFilePath))
            {
                try
                {
                    queueActions = JsonConvert.DeserializeObject<List<QueueAction>>(File.ReadAllText(queueFilePath))
                        ?? new List<QueueAction>();
                }
                catch
                {
                    queueActions = new List<QueueAction>();
                }
            }

            queueActions.Add(new QueueAction
            {
                Action = "add",
                GameId = rommId,
                GameName = selectedGame.Title ?? "",
                FsName = fileName,
                FsPath = remotePath,
                Timestamp = DateTime.UtcNow
            });

            var dir = Path.GetDirectoryName(queueFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(queueActions, Formatting.Indented);
            File.WriteAllText(queueFilePath, json);

            GameManagerLauncher.EnsureOpen();
        }

        public bool GetIsValidForGames(IGame[] selectedGames)
        {
            return false;
        }

        public void OnSelected(IGame[] selectedGames)
        {
        }
    }
}
