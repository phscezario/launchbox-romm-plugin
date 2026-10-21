using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class InstallStateReconcilerTests
    {
        private static DownloadItem Item(int id, DownloadStatus status, string error = null)
        {
            return new DownloadItem { GameId = id, Status = status, Error = error };
        }

        [Fact]
        public void Decide_PicksInstalledVerdictWrittenByAnotherInstance()
        {
            var memory = new[] { Item(5424, DownloadStatus.WaitingInstall) };
            var file = new Dictionary<int, DownloadItem>
            {
                { 5424, Item(5424, DownloadStatus.Installed) }
            };

            var actions = InstallStateReconciler.Decide(memory, file);

            var action = Assert.Single(actions);
            Assert.Equal(5424, action.GameId);
            Assert.Equal(DownloadStatus.Installed, action.TargetStatus);
        }

        [Fact]
        public void Decide_PicksFailedVerdictWithError()
        {
            var memory = new[] { Item(7, DownloadStatus.WaitingInstall) };
            var file = new Dictionary<int, DownloadItem>
            {
                { 7, Item(7, DownloadStatus.Failed, "Archive missing") }
            };

            var action = Assert.Single(InstallStateReconciler.Decide(memory, file));

            Assert.Equal(DownloadStatus.Failed, action.TargetStatus);
            Assert.Equal("Archive missing", action.Error);
        }

        [Fact]
        public void Decide_IgnoresItemsStillWaiting()
        {
            var memory = new[] { Item(1, DownloadStatus.WaitingInstall) };
            var file = new Dictionary<int, DownloadItem>
            {
                { 1, Item(1, DownloadStatus.WaitingInstall) }
            };

            Assert.Empty(InstallStateReconciler.Decide(memory, file));
        }

        [Fact]
        public void Decide_IgnoresItemsAlreadySettledInMemory()
        {
            var memory = new[] { Item(1, DownloadStatus.Installed) };
            var file = new Dictionary<int, DownloadItem>
            {
                { 1, Item(1, DownloadStatus.Installed) }
            };

            Assert.Empty(InstallStateReconciler.Decide(memory, file));
        }

        [Fact]
        public void Decide_IgnoresItemsOnlyPresentInTheFile()
        {
            var memory = new[] { Item(1, DownloadStatus.WaitingInstall) };
            var file = new Dictionary<int, DownloadItem>
            {
                { 2, Item(2, DownloadStatus.Installed) }
            };

            Assert.Empty(InstallStateReconciler.Decide(memory, file));
        }

        [Fact]
        public void Decide_NullInputs_ReturnEmpty()
        {
            Assert.Empty(InstallStateReconciler.Decide(null, null));
            Assert.Empty(InstallStateReconciler.Decide(new DownloadItem[0], null));
            Assert.Empty(InstallStateReconciler.Decide(null, new Dictionary<int, DownloadItem>()));
        }

        [Fact]
        public void Decide_DoesNotRevertARetryWithAStaleFileVerdict()
        {
            // The row is stamped by the Retry; the Failed in the file predates it
            // and is not a verdict this instance is missing.
            var now = new System.DateTime(2026, 10, 20, 8, 0, 0, System.DateTimeKind.Utc);

            var memory = new[]
            {
                new DownloadItem
                {
                    GameId = 5424,
                    Status = DownloadStatus.WaitingInstall,
                    StatusUpdatedAt = now
                }
            };
            var file = new Dictionary<int, DownloadItem>
            {
                {
                    5424,
                    new DownloadItem
                    {
                        GameId = 5424,
                        Status = DownloadStatus.Failed,
                        Error = "boom",
                        StatusUpdatedAt = now.AddMinutes(-1)
                    }
                }
            };

            Assert.Empty(InstallStateReconciler.Decide(memory, file));
        }

        [Fact]
        public void Decide_HandlesSeveralRows()
        {
            var memory = new[]
            {
                Item(1, DownloadStatus.WaitingInstall),
                Item(2, DownloadStatus.WaitingInstall),
                Item(3, DownloadStatus.Downloading)
            };
            var file = new Dictionary<int, DownloadItem>
            {
                { 1, Item(1, DownloadStatus.Installed) },
                { 2, Item(2, DownloadStatus.Failed, "boom") },
                { 3, Item(3, DownloadStatus.Installed) }
            };

            var actions = InstallStateReconciler.Decide(memory, file);

            Assert.Equal(2, actions.Count);
            Assert.Equal(new[] { 1, 2 }, actions.Select(a => a.GameId).ToArray());
        }
    }
}
