using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
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

                EnsureMissingRecords(installedService, gamesById);
            }
            catch (Exception ex)
            {
                RommLogger.LogException(ex);
            }
        }

        /// <summary>
        /// Writes back a true folder flag on the LaunchBox game when disk
        /// evidence overrode a stale sync flag. Idempotent; UI-layer helper so
        /// both the repair runner and the Game Manager share one writer.
        /// </summary>
        public static void BackfillFolderFlag(IGame game)
        {
            if (game == null) return;

            var field = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.IsFolderGame);
            if (field != null && field.Value == bool.TrueString) return;

            if (field == null)
            {
                field = game.AddNewCustomField();
                field.Name = GameCustomFields.IsFolderGame;
            }

            field.Value = bool.TrueString;
        }

        /// <summary>
        /// Creates records for games that are valid on disk but have no record
        /// at all (e.g. a previous install was never registered). Only acts
        /// when no record exists — active or uninstalled — so it can never
        /// resurrect an intentional uninstall while files remain.
        /// </summary>
        private static void EnsureMissingRecords(
            IInstalledGamesService installedService,
            Dictionary<int, IGame> gamesById)
        {
            var knownIds = new HashSet<int>(installedService.GetAll()
                .Where(r => r != null)
                .Select(r => r.RommGameId));

            var settings = RommPluginStorage.Load();
            if (settings == null || string.IsNullOrWhiteSpace(settings.RomsPath)) return;

            var created = 0;
            var changed = false;

            foreach (var kv in gamesById)
            {
                if (knownIds.Contains(kv.Key)) continue;

                var game = kv.Value;
                var fields = game.GetAllCustomFields()
                    .GroupBy(f => f.Name)
                    .ToDictionary(g => g.Key, g => g.Last().Value);

                fields.TryGetValue(GameCustomFields.RemotePath, out var remotePath);
                fields.TryGetValue(GameCustomFields.FileName, out var fileName);
                fields.TryGetValue(GameCustomFields.IsFolderGame, out var folderValue);
                if (string.IsNullOrWhiteSpace(remotePath) || string.IsNullOrWhiteSpace(fileName))
                    continue;

                var isFolderGame = InstallValidation.IsFolderGameEffective(
                    folderValue == bool.TrueString, settings.RomsPath, remotePath, fileName);
                if (isFolderGame && folderValue != bool.TrueString)
                {
                    BackfillFolderFlag(game);
                    RommLogger.Log($"[Repair] Game {kv.Key} '{game.Title}': corrected stale folder flag");
                }

                var installedPath = InstallValidation.ResolveInstalledPath(
                    settings.RomsPath, remotePath, fileName, isFolderGame);
                bool valid = isFolderGame
                    ? InstallValidation.IsFolderInstallValid(installedPath)
                    : InstallValidation.IsFileInstallValid(installedPath);
                if (!valid) continue;

                installedService.MarkInstalled(new InstalledGameRecord
                {
                    RommGameId = kv.Key,
                    Title = game.Title,
                    Platform = game.Platform,
                    Category = game.Genres?.FirstOrDefault(),
                    RemotePath = remotePath,
                    FileName = fileName,
                    InstalledPath = installedPath,
                    InstalledAt = DateTime.UtcNow,
                });
                game.ApplicationPath = installedPath;
                game.Installed = true;
                changed = true;
                created++;
                RommLogger.Log($"[Repair] Game {kv.Key} '{game.Title}': valid install found on disk, record created");
            }

            if (changed)
                PluginHelper.DataManager.Save();

            if (created > 0)
                RommLogger.Log($"[Repair] Completed: {created} missing record(s) created");
        }
    }
}
