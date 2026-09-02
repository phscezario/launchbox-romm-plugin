using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
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
    /// <summary>
    /// Context menu item that queues an installed game for uninstallation.
    /// </summary>
    public class RommUninstallMenuItem : RommMenuItem, IGameMenuItemPlugin
    {
        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("context.uninstall");

        /// <inheritdoc/>
        public bool SupportsMultipleGames => false;

        /// <inheritdoc/>
        public bool GetIsValidForGame(IGame selectedGame)
        {
            return RommGameHelpers.TryGetRommId(selectedGame, out _)
                && selectedGame.Installed == true;
        }

        /// <inheritdoc/>
        public override void OnSelected()
        {
        }

        /// <inheritdoc/>
        public void OnSelected(IGame selectedGame)
        {
            if (!RommGameHelpers.TryGetRommId(selectedGame, out var rommId))
            {
                return;
            }

            var pluginDir = RommPaths.PluginFolder;

            var queueFilePath = Path.Combine(pluginDir, RommConstants.DownloadQueueFile);

            if (File.Exists(queueFilePath))
            {
                try
                {
                    var existingQueue = JsonConvert.DeserializeObject<List<QueueAction>>(File.ReadAllText(queueFilePath));
                    if (existingQueue != null && existingQueue.Any(a => a.GameId == rommId && a.Action == "remove"))
                    {
                        using (var form = new ConfirmForm(LocaleManager.Get("uninstall.already_queued")))
                            form.ShowDialog();
                        return;
                    }
                }
                catch
                {
                }
            }

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
                            (i.Status == DownloadStatus.WaitingUninstall));
                        if (activeItem != null)
                        {
                            using (var form = new ConfirmForm(LocaleManager.Get("uninstall.already_queued")))
                                form.ShowDialog();
                            return;
                        }
                    }
                }
                catch
                {
                }
            }

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
                Action = "remove",
                GameId = rommId,
                GameName = selectedGame.Title ?? "",
                Timestamp = DateTime.UtcNow
            });

            var dir = Path.GetDirectoryName(queueFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(queueActions, Formatting.Indented);
            File.WriteAllText(queueFilePath, json);

            GameManagerLauncher.EnsureOpen();
        }

        /// <inheritdoc/>
        public bool GetIsValidForGames(IGame[] selectedGames)
        {
            return false;
        }

        /// <inheritdoc/>
        public void OnSelected(IGame[] selectedGames)
        {
        }
    }
}
