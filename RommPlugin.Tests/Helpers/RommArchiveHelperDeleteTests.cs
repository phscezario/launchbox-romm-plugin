using System;
using System.IO;
using RommPlugin.Core.Helpers;
using Xunit;

namespace RommPlugin.Tests.Helpers
{
    public class RommArchiveHelperDeleteTests : IDisposable
    {
        private readonly string _root;

        public RommArchiveHelperDeleteTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "gamedel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var attributes = File.GetAttributes(file);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                            File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                    }
                    catch
                    {
                    }
                }

                Directory.Delete(_root, true);
            }
            catch
            {
            }
        }

        [Fact]
        public void DeleteGamePath_DeletesFile()
        {
            var file = Path.Combine(_root, "game.wad");
            File.WriteAllText(file, "x");

            var outcome = RommArchiveHelper.DeleteGamePath(file);

            Assert.Equal(GamePathDeleteOutcome.Deleted, outcome);
            Assert.False(File.Exists(file));
        }

        [Fact]
        public void DeleteGamePath_DeletesTreeWithReadOnlyFiles()
        {
            var dir = Path.Combine(_root, "game");
            var sub = Path.Combine(dir, "_DLCs");
            Directory.CreateDirectory(sub);
            var locked = Path.Combine(sub, "a.pkg");
            File.WriteAllText(locked, "x");
            File.SetAttributes(locked, File.GetAttributes(locked) | FileAttributes.ReadOnly);

            var outcome = RommArchiveHelper.DeleteGamePath(dir);

            Assert.Equal(GamePathDeleteOutcome.Deleted, outcome);
            Assert.False(Directory.Exists(dir));
        }

        [Fact]
        public void DeleteGamePath_Fails_WhenFileLocked()
        {
            var dir = Path.Combine(_root, "locked");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "a.pkg");
            File.WriteAllText(file, "x");

            using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var outcome = RommArchiveHelper.DeleteGamePath(dir);

                Assert.Equal(GamePathDeleteOutcome.Failed, outcome);
                Assert.True(Directory.Exists(dir));
            }
        }

        [Fact]
        public void DeleteGamePath_AlreadyGone_WhenMissingOrBlank()
        {
            Assert.Equal(
                GamePathDeleteOutcome.AlreadyGone,
                RommArchiveHelper.DeleteGamePath(Path.Combine(_root, "nope")));
            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, RommArchiveHelper.DeleteGamePath(null));
            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, RommArchiveHelper.DeleteGamePath("  "));
        }

        [Theory]
        [InlineData("_temp_0112bd4a413b465fad6d67de4b6ba0ad", true)]
        [InlineData("Drakengard 3._tmp_0112bd4a413b465fad6d67de4b6ba0ad", true)]
        [InlineData("Drakengard 3", false)]
        [InlineData("_DLCs", false)]
        [InlineData("_temp_xyz", false)]
        [InlineData("game._tmp_xyz", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void IsTempStagingDirName_MatchesOnlyStrictTempPatterns(string name, bool expected)
        {
            Assert.Equal(expected, RommArchiveHelper.IsTempStagingDirName(name));
        }

        [Fact]
        public void CopyFileRobust_CopiesContent()
        {
            var src = Path.Combine(_root, "src.txt");
            var dest = Path.Combine(_root, "sub", "dest.txt");
            File.WriteAllText(src, "hello");

            RommArchiveHelper.CopyFileRobust(src, dest);

            Assert.Equal("hello", File.ReadAllText(dest));
        }

        [Fact]
        public void CopyFileRobust_OverwritesReadOnlyDest()
        {
            var src = Path.Combine(_root, "src.txt");
            var dest = Path.Combine(_root, "dest.txt");
            File.WriteAllText(src, "new");
            File.WriteAllText(dest, "old");
            File.SetAttributes(dest, File.GetAttributes(dest) | FileAttributes.ReadOnly);

            RommArchiveHelper.CopyFileRobust(src, dest);

            Assert.Equal("new", File.ReadAllText(dest));
        }

        [Fact]
        public void EnsureWritableDirectory_PassesOnWritableDir()
        {
            var dir = Path.Combine(_root, "w");
            Directory.CreateDirectory(dir);

            var exception = Record.Exception(() => RommArchiveHelper.EnsureWritableDirectory(dir));

            Assert.Null(exception);
        }

        [Fact]
        public void EnsureWritableDirectory_ThrowsWhenBlockedByFile()
        {
            var blocker = Path.Combine(_root, "blocker");
            File.WriteAllText(blocker, "x");
            var dir = Path.Combine(blocker, "sub");

            Assert.Throws<System.IO.IOException>(() => RommArchiveHelper.EnsureWritableDirectory(dir));
        }
    }
}
