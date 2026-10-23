using System;
using System.IO;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class GameFileDeleterTests : IDisposable
    {
        private readonly string _root;
        private readonly GameFileDeleter _deleter;

        public GameFileDeleterTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "filedeleter_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _deleter = new GameFileDeleter();
        }

        public void Dispose()
        {
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
        public void NullRecord_IsAlreadyGone()
        {
            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, _deleter.DeleteRecordPaths(null, _root));
        }

        [Fact]
        public void MissingPaths_IsAlreadyGone()
        {
            var record = new InstalledGameRecord
            {
                RommGameId = 1,
                Title = "Gone",
                InstalledPath = Path.Combine(_root, "nope"),
            };

            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, _deleter.DeleteRecordPaths(record, _root));
        }

        [Fact]
        public void ExistingFolder_IsDeleted()
        {
            var dir = Path.Combine(_root, "game");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "a.txt"), "x");
            var record = new InstalledGameRecord
            {
                RommGameId = 1,
                Title = "Game",
                InstalledPath = dir,
            };

            Assert.Equal(GamePathDeleteOutcome.Deleted, _deleter.DeleteRecordPaths(record, _root));
            Assert.False(Directory.Exists(dir));
        }

        [Fact]
        public void Failed_Dominates_Deleted()
        {
            var missing = Path.Combine(_root, "gone");
            var lockedDir = Path.Combine(_root, "romm", "x", "locked");
            Directory.CreateDirectory(lockedDir);
            var file = Path.Combine(lockedDir, "a.txt");
            File.WriteAllText(file, "x");
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var record = new InstalledGameRecord
                {
                    RommGameId = 1,
                    Title = "Game",
                    InstalledPath = missing,
                    RemotePath = "x",
                    FileName = "locked",
                };

                Assert.Equal(GamePathDeleteOutcome.Failed, _deleter.DeleteRecordPaths(record, _root));
                Assert.True(Directory.Exists(lockedDir));
            }
        }
    }
}
