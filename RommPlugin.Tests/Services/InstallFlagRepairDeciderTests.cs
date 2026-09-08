using System;
using System.IO;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class InstallFlagRepairDeciderTests : IDisposable
    {
        private readonly string _root;

        public InstallFlagRepairDeciderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "repair_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }

        private static InstalledGameRecord ActiveRecord(string installedPath)
        {
            return new InstalledGameRecord
            {
                RommGameId = 1,
                Title = "Game",
                InstalledPath = installedPath
            };
        }

        [Fact]
        public void Decide_PhantomInstall_RewritesFields()
        {
            var zip = Path.Combine(_root, "game.zip");
            File.WriteAllText(zip, "x");

            var decision = InstallFlagRepairDecider.Decide(ActiveRecord(zip), null, false);

            Assert.True(decision.RepairFields);
            Assert.True(decision.UpdateApplicationPath);
            Assert.False(decision.RequeueDownload);
        }

        [Fact]
        public void Decide_ConsistentInstall_NeedsNothing()
        {
            var zip = Path.Combine(_root, "game.zip");
            File.WriteAllText(zip, "x");

            var decision = InstallFlagRepairDecider.Decide(ActiveRecord(zip), zip, true);

            Assert.False(decision.NeedsAction);
        }

        [Fact]
        public void Decide_DanglingAppPath_UpdatesPathOnly()
        {
            var zip = Path.Combine(_root, "game.zip");
            File.WriteAllText(zip, "x");

            var decision = InstallFlagRepairDecider.Decide(
                ActiveRecord(zip), Path.Combine(_root, "gone.zip"), true);

            Assert.True(decision.RepairFields);
            Assert.True(decision.UpdateApplicationPath);
        }

        [Fact]
        public void Decide_MissingFiles_RequeuesDownload()
        {
            var decision = InstallFlagRepairDecider.Decide(
                ActiveRecord(Path.Combine(_root, "gone.zip")), null, false);

            Assert.True(decision.RequeueDownload);
            Assert.False(decision.RepairFields);
        }

        [Fact]
        public void Decide_UninstalledOrNullRecord_NeedsNothing()
        {
            var zip = Path.Combine(_root, "game.zip");
            File.WriteAllText(zip, "x");

            Assert.False(InstallFlagRepairDecider.Decide(null, null, false).NeedsAction);
            Assert.False(InstallFlagRepairDecider.Decide(
                new InstalledGameRecord { RommGameId = 1, InstalledPath = zip, UninstalledAt = DateTime.UtcNow },
                null, false).NeedsAction);
        }

        [Fact]
        public void Decide_FolderInstall_DirectoryCountsAsPresent()
        {
            var dir = Path.Combine(_root, "game");
            Directory.CreateDirectory(dir);

            var decision = InstallFlagRepairDecider.Decide(ActiveRecord(dir), null, false);

            Assert.True(decision.RepairFields);
            Assert.False(decision.RequeueDownload);
        }
    }
}
