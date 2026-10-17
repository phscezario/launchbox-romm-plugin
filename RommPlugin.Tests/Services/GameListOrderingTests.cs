using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class GameListOrderingTests
    {
        private static GameManagerRow Row(int id, string name, DownloadStatus? status)
        {
            var row = new GameManagerRow { GameId = id };
            if (status.HasValue)
                row.QueueItem = new DownloadItem { GameId = id, GameName = name, Status = status.Value };
            row.Installed = new InstalledGameRecord { RommGameId = id, Title = name };
            return row;
        }

        [Theory]
        [InlineData(DownloadStatus.Pending)]
        [InlineData(DownloadStatus.Downloading)]
        [InlineData(DownloadStatus.Paused)]
        [InlineData(DownloadStatus.Failed)]
        [InlineData(DownloadStatus.WaitingInstall)]
        [InlineData(DownloadStatus.WaitingUninstall)]
        public void IsTopPriority_TrueForMovingOrFailed(DownloadStatus status)
        {
            Assert.True(GameListOrdering.IsTopPriority(Row(1, "G", status)));
        }

        [Theory]
        [InlineData(DownloadStatus.Installed)]
        [InlineData(DownloadStatus.Completed)]
        [InlineData(DownloadStatus.Cancelled)]
        public void IsTopPriority_FalseForStable(DownloadStatus status)
        {
            Assert.False(GameListOrdering.IsTopPriority(Row(1, "G", status)));
        }

        [Fact]
        public void IsTopPriority_FalseForRowWithoutQueueOrNull()
        {
            Assert.False(GameListOrdering.IsTopPriority(Row(1, "G", null)));
            Assert.False(GameListOrdering.IsTopPriority(null));
        }

        [Fact]
        public void Order_TopGroupFirst_ThenAlphabeticalWithinEachGroup()
        {
            var rows = new List<GameManagerRow>
            {
                Row(1, "Zelda", DownloadStatus.Installed),
                Row(2, "Mario", DownloadStatus.Downloading),
                Row(3, "Ape Escape", DownloadStatus.Installed),
                Row(4, "Banjo", DownloadStatus.Failed),
                Row(5, "Crash", DownloadStatus.WaitingInstall),
            };

            var ordered = GameListOrdering.Order(rows);

            Assert.Equal(new[] { 4, 5, 2, 3, 1 }, ordered.Select(r => r.GameId));
        }

        [Fact]
        public void Order_TiesBreakById()
        {
            var rows = new List<GameManagerRow>
            {
                Row(9, "Same", DownloadStatus.Installed),
                Row(4, "Same", DownloadStatus.Installed),
            };

            var ordered = GameListOrdering.Order(rows);

            Assert.Equal(new[] { 4, 9 }, ordered.Select(r => r.GameId));
        }

        [Fact]
        public void Order_RowsWithoutQueue_AreStable()
        {
            var active = Row(1, "Alpha", DownloadStatus.Downloading);
            var bare = new GameManagerRow
            {
                GameId = 2,
                Installed = new InstalledGameRecord { RommGameId = 2, Title = "Beta" },
            };

            var ordered = GameListOrdering.Order(new List<GameManagerRow> { bare, active });

            Assert.Equal(new[] { 1, 2 }, ordered.Select(r => r.GameId));
        }

        [Fact]
        public void Order_HandlesNullInputAndNullElements()
        {
            Assert.Empty(GameListOrdering.Order(null));

            var rows = new List<GameManagerRow> { null, Row(1, "G", DownloadStatus.Installed) };
            Assert.Equal(new[] { 1 }, GameListOrdering.Order(rows).Select(r => r.GameId));
        }

        [Fact]
        public void SortName_UsesQueueNameOnQueueRows_AndTitleOtherwise()
        {
            Assert.Equal("Queue Name", Row(1, "Queue Name", DownloadStatus.Downloading).SortName);
            Assert.Equal("Title", Row(1, "Title", DownloadStatus.Installed).SortName);
        }
    }
}
