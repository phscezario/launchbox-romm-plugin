using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Decisions
{
    /// <summary>A verdict from the shared state file still missing in memory.</summary>
    public sealed class InstallReconcileAction
    {
        public int GameId { get; set; }
        public DownloadStatus TargetStatus { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Detects rows that the shared state file already settled while this
    /// process still shows them as "Installing".
    /// <para>
    /// The file is the only cross-process view of the install pipeline: when
    /// another LaunchBox instance ran the pass, this process's in-memory queue
    /// never saw the item leave <c>WaitingInstall</c> and the row (and the
    /// installed-games record) stayed wrong until the manager was reopened.
    /// Only the items this process knows about are reconciled; nothing is
    /// adopted from the file.
    /// </para>
    /// </summary>
    public static class InstallStateReconciler
    {
        public static List<InstallReconcileAction> Decide(
            IEnumerable<DownloadItem> memoryItems,
            IReadOnlyDictionary<int, DownloadItem> stateFromFile)
        {
            var actions = new List<InstallReconcileAction>();
            if (memoryItems == null || stateFromFile == null) return actions;

            foreach (var item in memoryItems)
            {
                if (item == null || !stateFromFile.TryGetValue(item.GameId, out var fileItem))
                    continue;

                // Same recency rule as the queue merge: a verdict older than the
                // local change (e.g. the Failed left behind by a Retry) is not a
                // verdict this process is missing.
                if (!DownloadStateMerger.IsPendingVerdict(item, fileItem))
                    continue;

                actions.Add(new InstallReconcileAction
                {
                    GameId = item.GameId,
                    TargetStatus = fileItem.Status,
                    Error = fileItem.Error
                });
            }

            return actions;
        }
    }
}
