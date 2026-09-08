using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.UI.Helpers
{
    /// <summary>
    /// Shared installed-flags reconciliation: rewrites LaunchBox-side
    /// ApplicationPath/Installed fields from installed-games records when the
    /// files exist on disk, or re-queues the download when they are gone.
    /// Silent (log only); safe to call from startup and from the Game Manager.
    /// </summary>
    public static class InstallFlagRepairRunner
    {
        /// <summary>
        /// Reconciles all active installed-games records with the LaunchBox-side fields.
        /// </summary>
        public static void RepairAll(
            IInstalledGamesService installedService,
            IDownloadQueueService queueService)
        {
            if (installedService == null || queueService == null) return;

            try
            {
                var records = installedService.GetAll()
                    .Where(r => r != null && !r.UninstalledAt.HasValue)
                    .ToList();

                if (records.Count == 0) return;

                var gamesById = new Dictionary<int, IGame>();
                foreach (var game in PluginHelper.DataManager.GetAllGames())
                {
                    if (game == null || game.Platform == null
                        || !game.Platform.StartsWith(RommConstants.PlatformPrefix))
                        continue;

                    var value = game.GetAllCustomFields()
                        .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;
                    if (int.TryParse(value, out var id) && !gamesById.ContainsKey(id))
                        gamesById[id] = game;
                }

                var repaired = 0;
                var requeued = 0;
                var changed = false;

                foreach (var record in records)
                {
                    if (!gamesById.TryGetValue(record.RommGameId, out var game))
                    {
                        RommLogger.Log($"[Repair] Game {record.RommGameId} '{record.Title}': not found in LaunchBox, skipped");
                        continue;
                    }

                    var decision = InstallFlagRepairDecider.Decide(record, game.ApplicationPath, game.Installed);

                    if (decision.RepairFields)
                    {
                        if (decision.UpdateApplicationPath)
                            game.ApplicationPath = record.InstalledPath;
                        game.Installed = true;
                        changed = true;
                        repaired++;
                        RommLogger.Log($"[Repair] Game {record.RommGameId} '{record.Title}': LaunchBox flags rewritten (path='{record.InstalledPath}')");
                    }
                    else if (decision.RequeueDownload)
                    {
                        if (string.IsNullOrWhiteSpace(record.FileName) || string.IsNullOrWhiteSpace(record.RemotePath))
                        {
                            RommLogger.Log($"[Repair] Game {record.RommGameId} '{record.Title}': files missing and record has no location info, skipped");
                            continue;
                        }

                        queueService.Enqueue(record.RommGameId, record.Title, record.FileName, record.RemotePath);
                        requeued++;
                        RommLogger.Log($"[Repair] Game {record.RommGameId} '{record.Title}': files missing, re-queued for download");
                    }
                }

                if (changed)
                    PluginHelper.DataManager.Save();

                if (repaired > 0 || requeued > 0)
                    RommLogger.Log($"[Repair] Completed: {repaired} flag(s) rewritten, {requeued} game(s) re-queued");
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }
    }
}
