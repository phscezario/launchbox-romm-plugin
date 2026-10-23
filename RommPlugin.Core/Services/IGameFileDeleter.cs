using RommPlugin.Core.Helpers;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Filesystem side of uninstall: tri-state deletes plus orphan sweeps.
    /// Keeps UI flows free of direct file logic; covered by unit tests.
    /// </summary>
    public interface IGameFileDeleter
    {
        /// <summary>Deletes one candidate path with per-outcome logging.</summary>
        GamePathDeleteOutcome DeleteCandidate(string path);

        /// <summary>
        /// Deletes every candidate path for a record. Failed dominates,
        /// then Deleted, else AlreadyGone. Mirrors the Game Manager order.
        /// </summary>
        GamePathDeleteOutcome DeleteRecordPaths(InstalledGameRecord record, string romsPath);

        /// <summary>Best-effort orphan sweep after an uninstall. Never throws.</summary>
        void CleanAfterUninstall(InstalledGameRecord record, string romsPath);
    }
}
