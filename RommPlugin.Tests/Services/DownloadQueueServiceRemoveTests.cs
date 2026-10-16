using System;
using System.IO;
using System.Linq;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class DownloadQueueServiceRemoveTests : IDisposable
    {
        private readonly string _root;
        private readonly string _stateFile;
        private readonly DownloadQueueService _service;

        public DownloadQueueServiceRemoveTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "queueremove_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _stateFile = Path.Combine(_root, "download-state.json");
            // Discard port: connection refused instantly, no network in tests.
            _service = new DownloadQueueService(_stateFile, _root, "http://127.0.0.1:9/");
        }

        public void Dispose()
        {
            try { _service?.Dispose(); } catch { }
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, true);
            }
            catch
            {
            }
        }

        [Fact]
        public void Remove_DropsPendingItem_AndPersists()
        {
            _service.Enqueue(1, "Game", "game.zip", "PS3");
            _service.Remove(1);

            Assert.Empty(_service.Items.Where(i => i.GameId == 1));
            var saved = File.ReadAllText(_stateFile);
            Assert.DoesNotContain("\"GameId\": 1", saved);
        }

        [Fact]
        public void Remove_DropsFailedItem()
        {
            _service.Enqueue(2, "Game", "game.zip", "PS3");
            _service.Items.First(i => i.GameId == 2).Status = DownloadStatus.Failed;

            _service.Remove(2);

            Assert.Empty(_service.Items.Where(i => i.GameId == 2));
        }

        [Fact]
        public void Remove_DeletesPartFile()
        {
            _service.Enqueue(3, "Game", "game.zip", "PS3");
            var part = _service.Items.First(i => i.GameId == 3).PartFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(part));
            File.WriteAllText(part, "partial");

            _service.Remove(3);

            Assert.False(File.Exists(part));
        }

        [Fact]
        public void Remove_MissingId_IsNoOp()
        {
            var exception = Record.Exception(() => _service.Remove(999));

            Assert.Null(exception);
        }

        [Fact]
        public void Retry_KeepsPartFile_ForResume()
        {
            _service.Enqueue(10, "Game", "game.zip", "PS3");
            WaitForStatus(10, DownloadStatus.Failed);
            var part = _service.Items.First(i => i.GameId == 10).PartFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(part));
            File.WriteAllText(part, "partial");

            _service.Retry(10);

            // The part file must survive: it is what the next attempt resumes from.
            Assert.True(File.Exists(part));
            Assert.NotNull(_service.Items.FirstOrDefault(i => i.GameId == 10));
        }

        [Fact]
        public void MarkInstallFailed_PersistsState()
        {
            _service.Enqueue(11, "Game", "game.zip", "PS3");
            WaitForStatus(11, DownloadStatus.Failed);
            _service.Items.First(i => i.GameId == 11).Status = DownloadStatus.WaitingInstall;

            _service.MarkInstallFailed(11, "boom");

            using (var reloaded = CreateService())
            {
                reloaded.LoadState();
                var item = reloaded.Items.FirstOrDefault(i => i.GameId == 11);
                Assert.NotNull(item);
                Assert.Equal(DownloadStatus.Failed, item.Status);
            }
        }

        private DownloadQueueService CreateService()
        {
            return new DownloadQueueService(
                Path.Combine(_root, "download-state.json"),
                _root,
                "http://127.0.0.1:9/");
        }

        private void WaitForStatus(int gameId, DownloadStatus status)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                var item = _service.Items.FirstOrDefault(i => i.GameId == gameId);
                if (item != null && item.Status == status)
                    return;
                System.Threading.Thread.Sleep(100);
            }

            throw new TimeoutException($"Timed out waiting for game {gameId} to reach {status}");
        }
    }
}
