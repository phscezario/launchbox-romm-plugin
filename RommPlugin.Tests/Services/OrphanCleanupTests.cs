using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class OrphanCleanupTests : IDisposable
    {
        private readonly string _root;

        public OrphanCleanupTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "orphan_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }

        private static void TouchStale(string path, TimeSpan age)
        {
            if (Directory.Exists(path))
                Directory.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
            else if (File.Exists(path))
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        }

        [Fact]
        public void IsZipReadable_RejectsTruncatedArchive()
        {
            var zip = Path.Combine(_root, "truncated.zip");
            File.WriteAllBytes(zip, new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 });
            Assert.False(RommArchiveHelper.IsZipReadable(zip));
        }

        [Fact]
        public void IsZipReadable_AcceptsValidArchive()
        {
            var zip = Path.Combine(_root, "valid.zip");
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("game.gbc");
                using (var w = new StreamWriter(entry.Open()))
                    w.Write("data");
            }
            Assert.True(RommArchiveHelper.IsZipReadable(zip));
            Assert.False(RommArchiveHelper.IsZipReadable(Path.Combine(_root, "missing.zip")));
        }

        [Fact]
        public void CleanInstallAttempt_RemovesOnlyThisGamesLeftovers()
        {
            var gameDir = Path.Combine(_root, "GBC");
            var neighborDir = Path.Combine(_root, "NES");
            Directory.CreateDirectory(gameDir);
            Directory.CreateDirectory(neighborDir);

            var copied = Path.Combine(gameDir, "game.gbc");
            File.WriteAllText(copied, "partial");
            var extractDir = Path.Combine(gameDir, "game");
            Directory.CreateDirectory(extractDir);
            File.WriteAllText(Path.Combine(extractDir, "x"), "partial");
            var part = Path.Combine(gameDir, "game.zip.part");
            File.WriteAllText(part, "partial");
            var staleTemp = Path.Combine(gameDir, "_temp_" + new string('a', 32));
            Directory.CreateDirectory(staleTemp);
            TouchStale(staleTemp, TimeSpan.FromHours(1));

            var neighborFile = Path.Combine(neighborDir, "keep.nes");
            File.WriteAllText(neighborFile, "keep");
            var freshTemp = Path.Combine(gameDir, "_temp_" + new string('b', 32));
            Directory.CreateDirectory(freshTemp);

            var result = RommOrphanCleanupService.CleanInstallAttempt(
                _root, 1, "Game",
                new[] { copied }, extractDir, true, part, null, false);

            Assert.False(File.Exists(copied));
            Assert.False(Directory.Exists(extractDir));
            Assert.False(File.Exists(part));
            Assert.False(Directory.Exists(staleTemp));
            Assert.True(File.Exists(neighborFile));
            Assert.True(Directory.Exists(freshTemp));
            Assert.Equal(4, result.TotalRemoved);
        }

        [Fact]
        public void CleanInstallAttempt_KeepsPreexistingExtractDir()
        {
            var gameDir = Path.Combine(_root, "GBC");
            Directory.CreateDirectory(gameDir);
            var extractDir = Path.Combine(gameDir, "game");
            Directory.CreateDirectory(extractDir);
            var keep = Path.Combine(extractDir, "save.sav");
            File.WriteAllText(keep, "keep");

            var result = RommOrphanCleanupService.CleanInstallAttempt(
                _root, 1, "Game", null, extractDir, false, null, null, false);

            Assert.True(File.Exists(keep));
            Assert.Equal(0, result.TotalRemoved);
        }

        [Fact]
        public void CleanInstallAttempt_NeverEscapesRoot()
        {
            var outside = Path.Combine(Path.GetTempPath(), "orphan_outside_" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(outside, "keep");
            try
            {
                var result = RommOrphanCleanupService.CleanInstallAttempt(
                    _root, 1, "Game", new[] { outside }, null, false, null, null, false);
                Assert.True(File.Exists(outside));
                Assert.Equal(0, result.TotalRemoved);
            }
            finally
            {
                try { File.Delete(outside); } catch { }
            }
        }

        [Fact]
        public void CleanFull_RemovesOnlyPluginOwnedPatterns()
        {
            var staleTemp = Path.Combine(_root, "_temp_" + new string('c', 32));
            Directory.CreateDirectory(staleTemp);
            TouchStale(staleTemp, TimeSpan.FromHours(1));
            var orphanPart = Path.Combine(_root, "old.zip.part");
            File.WriteAllText(orphanPart, "x");
            TouchStale(orphanPart, TimeSpan.FromHours(1));
            var knownPart = Path.Combine(_root, "active.zip.part");
            File.WriteAllText(knownPart, "x");
            var rom = Path.Combine(_root, "game.zip");
            File.WriteAllText(rom, "x");
            var userTemp = Path.Combine(_root, "_temp_user_stuff");
            Directory.CreateDirectory(userTemp);
            TouchStale(userTemp, TimeSpan.FromHours(1));

            var pluginFolder = Path.Combine(_root, "plugin");
            Directory.CreateDirectory(pluginFolder);
            var stateTmp = Path.Combine(pluginFolder, "download-state.abc.tmp");
            File.WriteAllText(stateTmp, "x");
            TouchStale(stateTmp, TimeSpan.FromHours(1));

            var result = RommOrphanCleanupService.CleanFull(
                _root, pluginFolder, new[] { knownPart });

            Assert.False(Directory.Exists(staleTemp));
            Assert.False(File.Exists(orphanPart));
            Assert.False(File.Exists(stateTmp));
            Assert.True(File.Exists(knownPart));
            Assert.True(File.Exists(rom));
            Assert.True(Directory.Exists(userTemp));
            Assert.Equal(3, result.TotalRemoved);
        }

        [Fact]
        public void TempStagingDirName_MatchesOnlyPluginPattern()
        {
            Assert.True(RommArchiveHelper.IsTempStagingDirName("_temp_" + new string('0', 32)));
            Assert.False(RommArchiveHelper.IsTempStagingDirName("_temp_user_stuff"));
            Assert.False(RommArchiveHelper.IsTempStagingDirName("_temp_short"));
            Assert.False(RommArchiveHelper.IsTempStagingDirName("game"));
        }
    }
}
