using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class DownloadStateMergerTests
    {
        private static DownloadItem Item(int id, DownloadStatus status, string error = null)
        {
            return new DownloadItem
            {
                GameId = id,
                GameName = "Game " + id,
                Status = status,
                Error = error,
                CompletedAt = status == DownloadStatus.Installed ? new DateTime(2026, 10, 19, 16, 20, 19, DateTimeKind.Utc) : (DateTime?)null
            };
        }

        [Fact]
        public void MergeForQueueSave_KeepsVerdictWrittenByAnotherInstance()
        {
            var local = new List<DownloadItem> { Item(5424, DownloadStatus.WaitingInstall) };
            var onDisk = new List<DownloadItem> { Item(5424, DownloadStatus.Installed) };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            var item = Assert.Single(merged);
            Assert.Equal(DownloadStatus.Installed, item.Status);
            Assert.NotNull(item.CompletedAt);

            // The in-memory row must stay untouched here: the UI still has to
            // reconcile it and register the installed game.
            Assert.Equal(DownloadStatus.WaitingInstall, local[0].Status);
        }

        [Fact]
        public void MergeForQueueSave_KeepsFailedVerdictAndError()
        {
            var local = new List<DownloadItem> { Item(7, DownloadStatus.WaitingInstall) };
            var onDisk = new List<DownloadItem> { Item(7, DownloadStatus.Failed, "Archive missing") };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            Assert.Equal(DownloadStatus.Failed, merged[0].Status);
            Assert.Equal("Archive missing", merged[0].Error);
        }

        [Fact]
        public void MergeForQueueSave_KeepsDownloadsStartedByAnotherInstance()
        {
            var local = new List<DownloadItem> { Item(1, DownloadStatus.Downloading) };
            var onDisk = new List<DownloadItem>
            {
                Item(1, DownloadStatus.Downloading),
                Item(2, DownloadStatus.Pending)
            };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            Assert.Equal(new[] { 1, 2 }, merged.Select(i => i.GameId).ToArray());
        }

        [Fact]
        public void MergeForQueueSave_DoesNotResurrectRemovedItems()
        {
            var local = new List<DownloadItem> { Item(1, DownloadStatus.Installed) };
            var onDisk = new List<DownloadItem>
            {
                Item(1, DownloadStatus.Installed),
                Item(2, DownloadStatus.Installed)
            };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, new[] { 2 });

            Assert.Equal(new[] { 1 }, merged.Select(i => i.GameId).ToArray());
        }

        [Fact]
        public void MergeForQueueSave_LocalActiveStatusWins()
        {
            var local = new List<DownloadItem> { Item(9, DownloadStatus.Downloading) };
            var onDisk = new List<DownloadItem> { Item(9, DownloadStatus.Installed) };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            Assert.Equal(DownloadStatus.Downloading, merged[0].Status);
        }

        [Fact]
        public void MergeForInstallSave_AppliesOnlyTouchedItems()
        {
            var touched = new Dictionary<int, DownloadItem>
            {
                { 1, Item(1, DownloadStatus.Installed) }
            };
            var onDisk = new List<DownloadItem>
            {
                Item(1, DownloadStatus.WaitingInstall),
                Item(2, DownloadStatus.Downloading)
            };

            var merged = DownloadStateMerger.MergeForInstallSave(touched, onDisk);

            Assert.Equal(DownloadStatus.Installed, merged.First(i => i.GameId == 1).Status);
            Assert.Equal(DownloadStatus.Downloading, merged.First(i => i.GameId == 2).Status);
        }

        [Fact]
        public void MergeForInstallSave_DoesNotResurrectItemsRemovedMeanwhile()
        {
            var touched = new Dictionary<int, DownloadItem>
            {
                { 1, Item(1, DownloadStatus.Installed) }
            };

            var merged = DownloadStateMerger.MergeForInstallSave(touched, new List<DownloadItem>());

            Assert.Empty(merged);
        }

        [Fact]
        public void IsPendingVerdict_OnlyForWaitingInstallAgainstSettledEntry()
        {
            Assert.True(DownloadStateMerger.IsPendingVerdict(
                Item(1, DownloadStatus.WaitingInstall), Item(1, DownloadStatus.Installed)));
            Assert.True(DownloadStateMerger.IsPendingVerdict(
                Item(1, DownloadStatus.WaitingInstall), Item(1, DownloadStatus.Failed)));
            Assert.False(DownloadStateMerger.IsPendingVerdict(
                Item(1, DownloadStatus.WaitingInstall), Item(1, DownloadStatus.WaitingInstall)));
            Assert.False(DownloadStateMerger.IsPendingVerdict(
                Item(1, DownloadStatus.Installed), Item(1, DownloadStatus.Installed)));
        }

        private static DownloadItem Stamped(int id, DownloadStatus status, DateTime stamp, string error = null)
        {
            var item = Item(id, status, error);
            item.StatusUpdatedAt = stamp;
            return item;
        }

        [Fact]
        public void MergeForQueueSave_DoesNotRevertARetryWithAStaleDiskVerdict()
        {
            // The user pressed Retry (local WaitingInstall, stamped now) while the
            // file still held the Failed it replaced. Promoting that verdict again
            // reverted the Retry a couple of seconds later.
            var now = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
            var local = new List<DownloadItem> { Stamped(5424, DownloadStatus.WaitingInstall, now) };
            var onDisk = new List<DownloadItem> { Stamped(5424, DownloadStatus.Failed, now.AddMinutes(-1), "boom") };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            Assert.Equal(DownloadStatus.WaitingInstall, merged[0].Status);
            Assert.Null(merged[0].Error);
        }

        [Fact]
        public void MergeForQueueSave_AppliesANewerDiskVerdict()
        {
            var now = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
            var local = new List<DownloadItem> { Stamped(5424, DownloadStatus.WaitingInstall, now.AddMinutes(-5)) };
            var onDisk = new List<DownloadItem> { Stamped(5424, DownloadStatus.Installed, now) };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            Assert.Equal(DownloadStatus.Installed, merged[0].Status);
            // The verdict keeps its own stamp, so a third write cannot date it
            // as older than the Retry it already superseded.
            Assert.Equal(now, merged[0].StatusUpdatedAt);
        }

        [Fact]
        public void MergeForQueueSave_LegacyLocalItemStillAcceptsTheDiskVerdict()
        {
            // Entries written before the stamp existed must keep the old
            // behaviour: the disk verdict wins.
            var local = new List<DownloadItem> { Item(5424, DownloadStatus.WaitingInstall) };
            var onDisk = new List<DownloadItem>
            {
                Stamped(5424, DownloadStatus.Installed, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc))
            };

            var merged = DownloadStateMerger.MergeForQueueSave(local, onDisk, null);

            Assert.Equal(DownloadStatus.Installed, merged[0].Status);
        }

        [Fact]
        public void MergeForInstallSave_KeepsTheVerdictStamp()
        {
            var now = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
            var touched = new Dictionary<int, DownloadItem>
            {
                { 1, Stamped(1, DownloadStatus.Installed, now) }
            };
            var onDisk = new List<DownloadItem> { Item(1, DownloadStatus.WaitingInstall) };

            var merged = DownloadStateMerger.MergeForInstallSave(touched, onDisk);

            Assert.Equal(DownloadStatus.Installed, merged[0].Status);
            Assert.Equal(now, merged[0].StatusUpdatedAt);
        }

        [Fact]
        public void IsPendingVerdict_RespectsTheStamp()
        {
            var now = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

            Assert.False(DownloadStateMerger.IsPendingVerdict(
                Stamped(1, DownloadStatus.WaitingInstall, now),
                Stamped(1, DownloadStatus.Failed, now.AddSeconds(-1))));

            Assert.True(DownloadStateMerger.IsPendingVerdict(
                Stamped(1, DownloadStatus.WaitingInstall, now.AddSeconds(-1)),
                Stamped(1, DownloadStatus.Installed, now)));
        }

        [Fact]
        public void IsPendingVerdict_TieGoesToTheLocalChange()
        {
            // UtcNow has ~15 ms granularity, so a Retry and the Failed it
            // replaced can share the same tick. The Retry must still win.
            var now = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

            Assert.False(DownloadStateMerger.IsPendingVerdict(
                Stamped(1, DownloadStatus.WaitingInstall, now),
                Stamped(1, DownloadStatus.Failed, now)));
        }

        [Fact]
        public void IsPendingVerdict_LegacyPairKeepsTheOldBehaviour()
        {
            // Neither side has a stamp: the disk verdict is still applied, as it
            // was before the field existed.
            Assert.True(DownloadStateMerger.IsPendingVerdict(
                Item(1, DownloadStatus.WaitingInstall),
                Item(1, DownloadStatus.Installed)));
        }
    }
}
