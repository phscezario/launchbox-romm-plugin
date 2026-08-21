using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommUninstallMenuItem : RommMenuItem, IGameMenuItemPlugin
    {
        public override string Caption => LocaleManager.Get("context.uninstall");

        public bool SupportsMultipleGames => false;

        public bool GetIsValidForGame(IGame selectedGame)
        {
            return RommGameHelpers.TryGetRommId(selectedGame, out _)
                && selectedGame.Installed == true;
        }

        public override void OnSelected()
        {
        }

        public void OnSelected(IGame selectedGame)
        {
            RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: game={selectedGame?.Title}");
            if (!RommGameHelpers.TryGetRommId(selectedGame, out var rommId))
            {
                RommLogger.Log("[DIAG] RommUninstallMenuItem.OnSelected: no rommId");
                return;
            }
            RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: rommId={rommId}");

            var pluginDir = RommPaths.PluginFolder;

            var queueFilePath = Path.Combine(pluginDir, "download-queue.json");

            if (File.Exists(queueFilePath))
            {
                try
                {
                    var existingQueue = JsonConvert.DeserializeObject<List<QueueAction>>(File.ReadAllText(queueFilePath));
                    if (existingQueue != null && existingQueue.Any(a => a.GameId == rommId && a.Action == "remove"))
                    {
                        RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: game {rommId} already in download-queue.json for uninstall");
                        MessageBox.Show(
                            LocaleManager.Get("uninstall.already_queued"),
                            LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: error reading download-queue.json: {ex.Message}");
                }
            }

            var stateFilePath = Path.Combine(pluginDir, "download-state.json");
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
                            RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: game {rommId} already in download-state.json with status WaitingUninstall");
                            MessageBox.Show(
                                LocaleManager.Get("uninstall.already_queued"),
                                LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: error reading download-state.json: {ex.Message}");
                }
            }

            RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: enqueueing game {rommId} to download-queue.json");

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
            RommLogger.Log($"[DIAG] RommUninstallMenuItem.OnSelected: wrote {json.Length} chars to {queueFilePath}");

            RommLogger.Log("[DIAG] RommUninstallMenuItem.OnSelected: opening Game Manager");
            RommGameManagerMenuItem.OpenOrBringToFront();
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
