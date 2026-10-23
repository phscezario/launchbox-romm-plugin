using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class UninstallPlanDeciderTests
    {
        private static GameManagerRow Row(int id, bool withRecord, bool uninstalled = false)
        {
            var row = new GameManagerRow { GameId = id };
            if (withRecord)
            {
                row.Installed = new InstalledGameRecord
                {
                    RommGameId = id,
                    Title = "G" + id,
                    UninstalledAt = uninstalled ? (DateTime?)DateTime.UtcNow : null,
                };
            }

            row.QueueItem = new DownloadItem { GameId = id, GameName = "G" + id };
            return row;
        }

        private static DownloadItem Item(int id, DownloadStatus status)
        {
            return new DownloadItem { GameId = id, GameName = "G" + id, Status = status };
        }

        [Fact]
        public void ActiveRecordsInSelection_ReturnsActiveRecords_InOrder()
        {
            var selected = new List<GameManagerRow>
            {
                Row(1, true),
                Row(2, false),
                Row(3, true),
            };

            var records = UninstallPlanDecider.ActiveRecordsInSelection(selected);

            Assert.Equal(new[] { 1, 3 }, records.Select(r => r.RommGameId));
        }

        [Fact]
        public void ActiveRecordsInSelection_SkipsStaleRecords()
        {
            var selected = new List<GameManagerRow> { Row(1, true, uninstalled: true) };

            var records = UninstallPlanDecider.ActiveRecordsInSelection(selected);

            Assert.Empty(records);
        }

        [Fact]
        public void ActiveRecords_FiltersNullAndUninstalled()
        {
            var all = new List<InstalledGameRecord>
            {
                new InstalledGameRecord { RommGameId = 1 },
                null,
                new InstalledGameRecord { RommGameId = 2, UninstalledAt = DateTime.UtcNow },
                new InstalledGameRecord { RommGameId = 3 },
            };

            var records = UninstallPlanDecider.ActiveRecords(all);

            Assert.Equal(new[] { 1, 3 }, records.Select(r => r.RommGameId));
        }

        [Fact]
        public void RowsWithoutRecordInSelection_OnlyRecordless()
        {
            var selected = new List<GameManagerRow>
            {
                Row(1, true),
                Row(2, false),
                Row(3, false),
            };

            var ids = UninstallPlanDecider.RowsWithoutRecordInSelection(selected);

            Assert.Equal(new[] { 2, 3 }, ids);
        }

        [Fact]
        public void DeadQueueDrops_OnlyFailedOrCancelled()
        {
            var selected = new List<GameManagerRow> { Row(1, false), Row(2, false), Row(3, false) };
            var queue = new List<DownloadItem>
            {
                Item(1, DownloadStatus.Failed),
                Item(2, DownloadStatus.Downloading),
                Item(3, DownloadStatus.Cancelled),
            };

            var drops = UninstallPlanDecider.DeadQueueDrops(selected, queue);

            Assert.Equal(new[] { 1, 3 }, drops.Select(i => i.GameId));
        }

        [Fact]
        public void DeadQueueDrops_DedupsAndKeepsSelectionOrder()
        {
            var selected = new List<GameManagerRow> { Row(2, false), Row(1, false), Row(2, false) };
            var queue = new List<DownloadItem>
            {
                Item(1, DownloadStatus.Failed),
                Item(2, DownloadStatus.Failed),
            };

            var drops = UninstallPlanDecider.DeadQueueDrops(selected, queue);

            Assert.Equal(new[] { 2, 1 }, drops.Select(i => i.GameId));
        }

        [Fact]
        public void NullInputs_AreEmpty()
        {
            Assert.Empty(UninstallPlanDecider.ActiveRecordsInSelection(null));
            Assert.Empty(UninstallPlanDecider.ActiveRecords(null));
            Assert.Empty(UninstallPlanDecider.RowsWithoutRecordInSelection(null));
            Assert.Empty(UninstallPlanDecider.DeadQueueDrops(null, null));
        }
    }
}
