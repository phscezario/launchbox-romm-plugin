using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Pure selection rules shared by the uninstall entry points. Keeps the
    /// "which records to uninstall" and "which dead queue rows to drop"
    /// decisions out of the WinForms layer and under unit test.
    /// </summary>
    public static class UninstallPlanDecider
    {
        /// <summary>
        /// Active (installed, not yet uninstalled) records for a selection, in
        /// row order. Rows without a record, or with a stale uninstalled one,
        /// are skipped.
        /// </summary>
        public static IList<InstalledGameRecord> ActiveRecordsInSelection(IReadOnlyList<GameManagerRow> selected)
        {
            var records = new List<InstalledGameRecord>();
            if (selected == null) return records;

            foreach (var row in selected)
            {
                if (row == null || !row.IsInstalledActive) continue;
                records.Add(row.Installed);
            }

            return records;
        }

        /// <summary>
        /// Active records out of a full record list, in record order. Null
        /// entries and already-uninstalled records are skipped.
        /// </summary>
        public static IList<InstalledGameRecord> ActiveRecords(IReadOnlyList<InstalledGameRecord> all)
        {
            var records = new List<InstalledGameRecord>();
            if (all == null) return records;

            foreach (var record in all)
            {
                if (record == null || record.UninstalledAt.HasValue) continue;
                records.Add(record);
            }

            return records;
        }

        /// <summary>
        /// Selected rows with no installed record at all (adoption candidates),
        /// in row order. Rows that already carry a record — active or stale —
        /// are skipped, mirroring the Game Manager behavior.
        /// </summary>
        public static IList<int> RowsWithoutRecordInSelection(IReadOnlyList<GameManagerRow> selected)
        {
            var ids = new List<int>();
            if (selected == null) return ids;

            foreach (var row in selected)
            {
                if (row == null || row.Installed != null) continue;
                ids.Add(row.GameId);
            }

            return ids;
        }

        /// <summary>
        /// Queue items to drop for a selection that has nothing installed:
        /// Failed/Cancelled entries whose id is selected, first match per id,
        /// in selection order. Active transfers are left alone (use cancel).
        /// </summary>
        public static IList<DownloadItem> DeadQueueDrops(
            IReadOnlyList<GameManagerRow> selected,
            IReadOnlyList<DownloadItem> queueItems)
        {
            var drops = new List<DownloadItem>();
            if (selected == null || queueItems == null) return drops;

            var seen = new HashSet<int>();
            foreach (var row in selected)
            {
                if (row == null || !seen.Add(row.GameId)) continue;

                var item = queueItems.FirstOrDefault(i => i != null && i.GameId == row.GameId);
                if (item == null) continue;
                if (item.Status != DownloadStatus.Failed && item.Status != DownloadStatus.Cancelled) continue;

                drops.Add(item);
            }

            return drops;
        }
    }
}
