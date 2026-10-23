using System;
using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Merges one process's view of the download queue with the state file that
    /// every LaunchBox instance shares.
    /// <para>
    /// Two instances can write the same file: the queue owns the transfer
    /// statuses, the install service owns the verdicts (Installed/Failed) of
    /// items that were waiting to install. Writing a plain in-memory snapshot
    /// overwrote the other side — an install verdict vanished and the row stayed
    /// "Installing" with the archive already consumed, or a download started by
    /// the other instance disappeared from the queue.
    /// </para>
    /// </summary>
    public static class DownloadStateMerger
    {
        /// <summary>
        /// State for <c>DownloadQueueService.SaveState</c>: local items win,
        /// except that a verdict already written to disk is kept. Items that
        /// only exist on disk (started by another instance) survive, unless this
        /// instance explicitly removed them.
        /// </summary>
        public static List<DownloadItem> MergeForQueueSave(
            IReadOnlyList<DownloadItem> localItems,
            IReadOnlyList<DownloadItem> onDiskItems,
            IReadOnlyCollection<int> removedIds)
        {
            var removed = new HashSet<int>(removedIds ?? (IReadOnlyCollection<int>)new int[0]);
            var diskById = IndexByGameId(onDiskItems);
            var localIds = new HashSet<int>();
            var merged = new List<DownloadItem>();

            foreach (var item in localItems ?? (IReadOnlyList<DownloadItem>)new DownloadItem[0])
            {
                if (item == null) continue;

                localIds.Add(item.GameId);
                diskById.TryGetValue(item.GameId, out var disk);
                merged.Add(ApplyInstallVerdict(item, disk));
            }

            foreach (var disk in onDiskItems ?? (IReadOnlyList<DownloadItem>)new DownloadItem[0])
            {
                if (disk == null) continue;
                if (localIds.Contains(disk.GameId)) continue;
                if (removed.Contains(disk.GameId)) continue;

                merged.Add(disk);
            }

            return merged;
        }

        /// <summary>
        /// State for the install service: only the items it touched overwrite a
        /// fresh disk snapshot, so a download that started (or progressed) while
        /// the extraction ran is not rolled back. An item touched but no longer
        /// on disk was removed by the user meanwhile and is not resurrected.
        /// </summary>
        public static List<DownloadItem> MergeForInstallSave(
            IReadOnlyDictionary<int, DownloadItem> touchedItems,
            IReadOnlyList<DownloadItem> onDiskItems)
        {
            var merged = new List<DownloadItem>();

            foreach (var disk in onDiskItems ?? (IReadOnlyList<DownloadItem>)new DownloadItem[0])
            {
                if (disk == null) continue;

                if (touchedItems != null &&
                    touchedItems.TryGetValue(disk.GameId, out var touched) &&
                    touched != null)
                {
                    var copy = disk.ShallowClone();
                    copy.Status = touched.Status;
                    copy.Error = touched.Error;
                    copy.CompletedAt = touched.CompletedAt;
                    // The stamp travels with the verdict, or the next queue save
                    // in another instance would see a verdict it cannot date and
                    // could reapply it over a newer local change.
                    copy.StatusUpdatedAt = touched.StatusUpdatedAt;
                    merged.Add(copy);
                }
                else
                {
                    merged.Add(disk);
                }
            }

            return merged;
        }

        /// <summary>
        /// True when the disk entry is a verdict this process has not applied
        /// yet: the item was waiting to install and another instance already
        /// finished (Installed) or rejected (Failed) it, after the local status
        /// last changed.
        /// </summary>
        /// <remarks>
        /// The timestamp is what keeps a user Retry from being reverted: Retry
        /// stamps the local item, so the older Failed already on disk no longer
        /// counts. Entries written before the stamp existed count as older than
        /// any local change, and a local item with no stamp (loaded from an old
        /// file) still accepts the disk verdict exactly as before.
        /// </remarks>
        public static bool IsPendingVerdict(DownloadItem local, DownloadItem onDisk)
        {
            if (local == null || onDisk == null) return false;
            if (local.Status != DownloadStatus.WaitingInstall) return false;

            if (onDisk.Status != DownloadStatus.Installed &&
                onDisk.Status != DownloadStatus.Failed)
                return false;

            if (local.StatusUpdatedAt != default(DateTime))
            {
                if (onDisk.StatusUpdatedAt == default(DateTime)) return false;
                // Strictly newer: on a tie the local change wins, or a Retry
                // written in the same clock tick as the old verdict loses.
                if (onDisk.StatusUpdatedAt <= local.StatusUpdatedAt) return false;
            }

            return true;
        }

        private static DownloadItem ApplyInstallVerdict(DownloadItem local, DownloadItem onDisk)
        {
            if (!IsPendingVerdict(local, onDisk)) return local;

            var copy = local.ShallowClone();
            copy.Status = onDisk.Status;
            copy.Error = onDisk.Error;
            copy.CompletedAt = onDisk.CompletedAt;
            copy.StatusUpdatedAt = onDisk.StatusUpdatedAt;
            return copy;
        }

        private static Dictionary<int, DownloadItem> IndexByGameId(IReadOnlyList<DownloadItem> items)
        {
            var index = new Dictionary<int, DownloadItem>();

            if (items == null) return index;

            foreach (var item in items)
            {
                if (item != null)
                    index[item.GameId] = item;
            }

            return index;
        }
    }
}
