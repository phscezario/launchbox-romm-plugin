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

            // Detach the LaunchBox references first, exactly like the Game
            // Manager: even a failed delete leaves the UI entry cleared so the
            // user is not left with a half-removed row.
            _gateway.ClearForUninstall(record.RommGameId, record.InstalledPath);

            var outcome = _deleter.DeleteRecordPaths(record, romsPath);
            if (outcome == GamePathDeleteOutcome.Failed)
                return GamePathDeleteOutcome.Failed;

            _installedService.MarkUninstalled(record.RommGameId);
            _queueService.Remove(record.RommGameId);
            _deleter.CleanAfterUninstall(record, romsPath);

            return outcome;
        }
    }
}
