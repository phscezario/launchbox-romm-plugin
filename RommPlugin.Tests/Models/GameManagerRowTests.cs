using System.Collections.Generic;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    public class GameManagerRowTests
    {
        [Fact]
        public void Merge_InstalledQueuePlusRecord_YieldsSingleRow()
        {
            var queue = new List<DownloadItem>
            {
                new DownloadItem { GameId = 7, GameName = "Zelda", Status = DownloadStatus.Installed }
            };
            var installed = new List<InstalledGameRecord>
            {
                new InstalledGameRecord { RommGameId = 7, Title = "Zelda" }
            };

            var rows = GameManagerRow.Merge(queue, installed);

            Assert.Single(rows);
            Assert.Equal(7, rows[0].GameId);
            Assert.NotNull(rows[0].QueueItem);
            Assert.NotNull(rows[0].Installed);
            Assert.False(rows[0].ShowsQueueDetails);
            Assert.True(rows[0].IsInstalledActive);
        }

        [Fact]
        public void Merge_ActiveDownloadPlusRecord_YieldsSingleRowShowingQueue()
        {
            var queue = new List<DownloadItem>
            {
                new DownloadItem { GameId = 3, GameName = "Mario", Status = DownloadStatus.Downloading }
            };
            var installed = new List<InstalledGameRecord>
            {
                new InstalledGameRecord { RommGameId = 3, Title = "Mario" }
            };

            var rows = GameManagerRow.Merge(queue, installed);

            Assert.Single(rows);
            Assert.True(rows[0].ShowsQueueDetails);
            Assert.True(rows[0].IsInstalledActive);
        }

        [Fact]
        public void Merge_DisjointSets_YieldsOneRowEach()
        {
            var queue = new List<DownloadItem>
            {
                new DownloadItem { GameId = 1, GameName = "A", Status = DownloadStatus.Pending }
            };
            var installed = new List<InstalledGameRecord>
            {
                new InstalledGameRecord { RommGameId = 2, Title = "B" }
            };

            var rows = GameManagerRow.Merge(queue, installed);

            Assert.Equal(2, rows.Count);
        }

        [Fact]
        public void Merge_EmptyInputs_YieldsEmpty()
        {
            Assert.Empty(GameManagerRow.Merge(null, null));
        }
    }
}
