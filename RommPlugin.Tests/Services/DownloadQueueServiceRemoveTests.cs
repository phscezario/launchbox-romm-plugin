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
        public void Remove_DeletesPartFile_ForDeadItem()
        {
            _service.Enqueue(3, "Game", "game.zip", "PS3");
            WaitForStatus(3, DownloadStatus.Failed);
            var part = _service.Items.First(i => i.GameId == 3).PartFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(part));
            File.WriteAllText(part, "partial");

            _service.Remove(3);

            // A dead row has nothing worth resuming.
            Assert.False(File.Exists(part));
        }

        [Fact]
        public void Remove_KeepsPartFile_ForActiveItem()
        {
            _service.Enqueue(4, "Game", "game.zip", "PS3");
            WaitForStatus(4, DownloadStatus.Failed);
            var item = _service.Items.First(i => i.GameId == 4);
            var part = item.PartFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(part));
            File.WriteAllText(part, "partial");

            // Simulate a queued/in-flight transfer. The background task has
            // already settled, so nothing overwrites this status before Remove.
            item.Status = DownloadStatus.Downloading;

            _service.Remove(4);

            // Dropping the row must never destroy a resumable large download.
            Assert.True(File.Exists(part));
            Assert.Empty(_service.Items.Where(i => i.GameId == 4));
        }

        [Fact]
        public void Remove_UnknownId_DoesNotTouchExistingRows()
        {
            _service.Enqueue(5, "Keep", "keep.zip", "PS3");
            WaitForStatus(5, DownloadStatus.Failed);

            _service.Remove(999);

            // Dropping an id that is not queued must not rewrite the file into an
            // empty state and lose the rows it does hold.
            var saved = File.ReadAllText(_stateFile);
            Assert.Contains("\"GameId\": 5", saved);
            Assert.DoesNotContain("999", saved);
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

        [Fact]
        public void SaveState_DoesNotReplaceAnUnreadableFile()
        {
            // skip instead of writing a memory-only snapshot that erases every
            // row the other LaunchBox instance owns.
            File.WriteAllText(_stateFile, "{ not json");

            _service.SaveState();

            Assert.Equal("{ not json", File.ReadAllText(_stateFile));
        }

        [Fact]
        public void LoadState_KeepsADownloadRunningInThisProcess()
        {
            _service.Enqueue(20, "Game", "game.zip", "PS3");
            WaitForStatus(20, DownloadStatus.Failed);

            // Simulate a live transfer: the background task already settled, so
            // nothing overwrites this status before LoadState.
            var live = _service.Items.First(i => i.GameId == 20);
            live.Status = DownloadStatus.Downloading;

            // The file still lists it as queued (older save / other instance).
            // Re-adding it as Pending would start a second worker on the same
            // .part, which fails instantly and shows the row as Failed.
            File.WriteAllText(
                _stateFile,
                "{\"Items\":[{\"GameId\":20,\"GameName\":\"Game\",\"FsName\":\"game.zip\",\"FsPath\":\"PS3\",\"Status\":0}]}");

            _service.LoadState();

            var reloaded = Assert.Single(_service.Items.Where(i => i.GameId == 20));
            Assert.Equal(DownloadStatus.Downloading, reloaded.Status);
        }

        [Fact]
        public void LoadState_DoesNotResurrectAnItemWhoseSaveWasSkipped()
        {
            _service.Enqueue(30, "Game", "game.zip", "PS3");
            WaitForStatus(30, DownloadStatus.Failed);
            // Wait for the worker's own save to land, or it would rewrite a valid
            // file after we corrupt it and the removal would persist by accident.
            WaitForStateFileToContain(30);

            // Make the shared file unreadable so Remove cannot persist: the
            // removal stays in memory, which is what happens on a transient IO
            // failure while another instance replaces the file.
            File.WriteAllText(_stateFile, "{ not json");
            _service.Remove(30);

            // The save was skipped, so the removal is memory-only: that is the
            // situation this test exists to cover.
            Assert.Equal("{ not json", File.ReadAllText(_stateFile));

            // The file becomes readable again with the removed row still in it.
            File.WriteAllText(
                _stateFile,
                "{\"Items\":[{\"GameId\":30,\"GameName\":\"Game\",\"FsName\":\"game.zip\",\"FsPath\":\"PS3\",\"Status\":4}]}");

            _service.LoadState();

            Assert.Empty(_service.Items.Where(i => i.GameId == 30));
        }

        [Fact]
        public void LoadState_GivesARestoredPendingItemAToken()
        {
            // Cts is [JsonIgnore]: without one, StartNext dereferences a null
            // token and the resumed download fails as a random error.
            File.WriteAllText(
                _stateFile,
                "{\"Items\":[{\"GameId\":40,\"GameName\":\"Game\",\"FsName\":\"game.zip\",\"FsPath\":\"PS3\",\"Status\":0}]}");

            _service.LoadState();

            var item = Assert.Single(_service.Items.Where(i => i.GameId == 40));
            Assert.NotNull(item.Cts);
        }

        [Fact]
        public void InstallPending_ClearsAPreviousError()
        {
            _service.Enqueue(50, "Game", "game.zip", "PS3");
            WaitForStatus(50, DownloadStatus.Failed);
            var item = _service.Items.First(i => i.GameId == 50);
            item.Status = DownloadStatus.WaitingInstall;
            item.Error = "old failure";

            _service.InstallPending(50);

            Assert.Equal(DownloadStatus.Installed, item.Status);
            Assert.Null(item.Error);
        }

        [Fact]
        public void MarkInstallFailed_ClearsAPreviousCompletion()
        {
            _service.Enqueue(51, "Game", "game.zip", "PS3");
            WaitForStatus(51, DownloadStatus.Failed);
            var item = _service.Items.First(i => i.GameId == 51);
            item.Status = DownloadStatus.WaitingInstall;
            item.CompletedAt = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

            _service.MarkInstallFailed(51, "boom");

            Assert.Equal(DownloadStatus.Failed, item.Status);
            Assert.Null(item.CompletedAt);
            Assert.Equal("boom", item.Error);
        }

        [Fact]
        public void Enqueue_ClearsAPendingRemovalSoTheRowSurvivesAReload()
        {
            _service.Enqueue(31, "Game", "game.zip", "PS3");
            WaitForStatus(31, DownloadStatus.Failed);
            WaitForStateFileToContain(31);

            // Remove while the file is unreadable: the removal stays in memory,
            // which is what happens on a transient IO failure.
            File.WriteAllText(_stateFile, "{ not json");
            _service.Remove(31);
            Assert.Equal("{ not json", File.ReadAllText(_stateFile));

            // The user queues the same game again before the removal landed.
            _service.Enqueue(31, "Game", "game.zip", "PS3");
            WaitForStatus(31, DownloadStatus.Failed);

            File.WriteAllText(
                _stateFile,
                "{\"Items\":[{\"GameId\":31,\"GameName\":\"Game\",\"FsName\":\"game.zip\",\"FsPath\":\"PS3\",\"Status\":4}]}");

            _service.LoadState();

            // The stale removal marker would drop this row on the next open.
            Assert.Single(_service.Items.Where(i => i.GameId == 31));
        }

        [Fact]
        public void Remove_DropsARowThatOnlyExistsInTheFile()
        {
            // Written by another instance (or before a reload): the row is not in
            // this service's memory, so the early return used to leave it on disk
            // and every open reprocessed the same request.
            File.WriteAllText(
                _stateFile,
                "{\"Items\":[{\"GameId\":60,\"GameName\":\"Game\",\"FsName\":\"game.zip\",\"FsPath\":\"PS3\",\"Status\":7}]}");

            Assert.Empty(_service.Items);

            _service.Remove(60);

            var saved = File.ReadAllText(_stateFile);
            Assert.DoesNotContain("\"GameId\": 60", saved);
        }

        private void WaitForStateFileToContain(int gameId)
        {
            var marker = "\"GameId\": " + gameId;
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    if (File.Exists(_stateFile) && File.ReadAllText(_stateFile).Contains(marker))
                        return;
                }
                catch (IOException)
                {
                    // The file is being replaced right now; retry.
                }

                System.Threading.Thread.Sleep(50);
            }

            throw new TimeoutException($"Timed out waiting for the state file to persist game {gameId}");
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
