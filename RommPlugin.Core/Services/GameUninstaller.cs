using RommPlugin.Core.Helpers;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Default <see cref="IGameUninstaller"/>: the single place that performs
    /// the shared per-record uninstall sequence, replacing the copy-pasted
    /// orchestration that used to live in the Game Manager.
    /// </summary>
    public sealed class GameUninstaller : IGameUninstaller
    {
        private readonly ILaunchBoxGameGateway _gateway;
        private readonly IGameFileDeleter _deleter;
        private readonly IInstalledGamesService _installedService;
        private readonly IDownloadQueueService _queueService;

        public GameUninstaller(
            ILaunchBoxGameGateway gateway,
            IGameFileDeleter deleter,
            IInstalledGamesService installedService,
            IDownloadQueueService queueService)
        {
            _gateway = gateway;
            _deleter = deleter;
            _installedService = installedService;
            _queueService = queueService;
        }

        public GamePathDeleteOutcome Uninstall(InstalledGameRecord record, string romsPath)
        {
            if (record == null)
                return GamePathDeleteOutcome.AlreadyGone;

            // Additional applications (DLC/loaders) are listed in a config file
            // inside the installed folder, so they can only be identified while
            // that folder still exists: clear them before deleting anything.
            // Everything else waits for the delete result.
            _gateway.ClearAdditionalApplications(record.RommGameId, record.InstalledPath);

            var outcome = _deleter.DeleteRecordPaths(record, romsPath);
            if (outcome == GamePathDeleteOutcome.Failed)
            {
                // Leave the record, the queue entry and the LaunchBox
                // ApplicationPath intact: a failed delete must not detach the
                // game, otherwise it is half-removed (files on disk, nothing in
                // the UI to retry from).
                return GamePathDeleteOutcome.Failed;
            }

            // Detach the LaunchBox references only once the files are gone (or
            // were already gone).
            _gateway.DetachForUninstall(record.RommGameId);
            _installedService.MarkUninstalled(record.RommGameId);
            _queueService.Remove(record.RommGameId);
            _deleter.CleanAfterUninstall(record, romsPath);

            return outcome;
        }
    }
}
