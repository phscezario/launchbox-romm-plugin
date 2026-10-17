using System.Collections.Generic;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class InstallOutcomeClassifierTests
    {
        private static DownloadItem Item(DownloadStatus status)
        {
            return new DownloadItem { GameId = 1, Status = status };
        }

        [Fact]
        public void PartitionsProcessed_And_Failed()
        {
            var before = new Dictionary<int, DownloadItem>
            {
                [1] = Item(DownloadStatus.WaitingInstall),
                [2] = Item(DownloadStatus.WaitingInstall),
                [3] = Item(DownloadStatus.WaitingInstall),
                [4] = Item(DownloadStatus.Downloading),
            };
            var after = new Dictionary<int, DownloadItem>
            {
                [1] = Item(DownloadStatus.Installed),
                [2] = Item(DownloadStatus.Failed),
                [3] = Item(DownloadStatus.WaitingInstall),
            };

            var outcome = InstallOutcomeClassifier.Classify(before, after);

            Assert.Equal(new[] { 1 }, outcome.ProcessedIds);
            Assert.Equal(new[] { 2 }, outcome.FailedIds);
        }

        [Fact]
        public void MissingAfter_IsIgnored()
        {
            var before = new Dictionary<int, DownloadItem> { [1] = Item(DownloadStatus.WaitingInstall) };

            var outcome = InstallOutcomeClassifier.Classify(
                before, new Dictionary<int, DownloadItem>());

            Assert.True(outcome.ProcessedIds.Count == 0);
            Assert.True(outcome.FailedIds.Count == 0);
        }

        [Fact]
        public void NullInputs_YieldEmptyOutcome()
        {
            var outcome = InstallOutcomeClassifier.Classify(null, null);

            Assert.True(outcome.ProcessedIds.Count == 0);
            Assert.True(outcome.FailedIds.Count == 0);
        }
    }
}
