using System.IO;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Default <see cref="IGameFileDeleter"/> on top of <see cref="RommArchiveHelper"/>
    /// and <see cref="RommOrphanCleanupService"/>. Log lines mirror the Game
    /// Manager ones so existing log-based checks keep working.
    /// </summary>
    public sealed class GameFileDeleter : IGameFileDeleter
    {
        public GamePathDeleteOutcome DeleteCandidate(string path)
        {
            var outcome = RommArchiveHelper.DeleteGamePath(path);
            switch (outcome)
            {
                case GamePathDeleteOutcome.Deleted:
                    RommLogger.Log($"[Uninstall] Deleted: {path}");
                    break;
                case GamePathDeleteOutcome.Failed:
                    RommLogger.LogError($"[Uninstall] Failed to delete {path}: locked or access denied");
                    break;
                default:
                    RommLogger.Log($"[Uninstall] Not found on disk: {path}");
                    break;
            }

            return outcome;
        }

        public GamePathDeleteOutcome DeleteRecordPaths(InstalledGameRecord record, string romsPath)
        {
            if (record == null)
                return GamePathDeleteOutcome.AlreadyGone;

            RommLogger.Log($"[Uninstall] Trying to delete files for: {record.Title} (GameId={record.RommGameId})");

            bool anyDeleted = false;
            bool anyFailed = false;

            var groups = DeleteCandidateListBuilder.Build(
                record.InstalledPath, romsPath, record.RemotePath, record.FileName, PathExists);

            foreach (var group in groups)
            {
                RommLogger.Log($"[Uninstall] Trying {group.Label}: {group.DescribedPath}");
                foreach (var path in group.Paths)
                {
                    var outcome = DeleteCandidate(path);
                    if (outcome == GamePathDeleteOutcome.Failed)
                        anyFailed = true;
                    else if (outcome == GamePathDeleteOutcome.Deleted)
                        anyDeleted = true;
                }
            }

            // Failed dominates and is never short-circuited: every candidate is
            // probed first, exactly like the Game Manager, so a locked file never
            // masks the removal of its siblings.
            if (anyFailed)
                return GamePathDeleteOutcome.Failed;

            if (!anyDeleted)
                RommLogger.Log($"[Uninstall] No files found to delete for: {record.Title}");

            return anyDeleted ? GamePathDeleteOutcome.Deleted : GamePathDeleteOutcome.AlreadyGone;
        }

        public void CleanAfterUninstall(InstalledGameRecord record, string romsPath)
        {
            if (record == null) return;

            try
            {
                RommOrphanCleanupService.CleanUninstallLeftovers(
                    romsPath,
                    record.RommGameId,
                    record.Title,
                    record.InstalledPath);
            }
            catch (System.Exception ex)
            {
                RommLogger.LogError($"[Uninstall] Cleanup after uninstall failed: {ex.Message}");
            }
        }

        private static bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }
    }
}
