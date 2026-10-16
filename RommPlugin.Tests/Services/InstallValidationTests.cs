using System;
using System.IO;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class InstallValidationTests
    {
        private static string NewTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "installvalid_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public void FolderInstall_Valid_WhenFolderHasConfig()
        {
            var dir = NewTempDir();
            try
            {
                File.WriteAllText(Path.Combine(dir, "_launchbox.json"), "{}");

                Assert.True(InstallValidation.IsFolderInstallValid(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void FolderInstall_Invalid_WhenFolderLacksConfig()
        {
            var dir = NewTempDir();
            try
            {
                Assert.False(InstallValidation.IsFolderInstallValid(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void FolderInstall_Invalid_WhenMissingOrBlank()
        {
            Assert.False(InstallValidation.IsFolderInstallValid(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
            Assert.False(InstallValidation.IsFolderInstallValid(null));
            Assert.False(InstallValidation.IsFolderInstallValid("  "));
        }

        [Fact]
        public void FileInstall_Valid_WhenFileExists()
        {
            var file = Path.GetTempFileName();
            try
            {
                Assert.True(InstallValidation.IsFileInstallValid(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void FileInstall_Invalid_WhenMissingOrBlank()
        {
            Assert.False(InstallValidation.IsFileInstallValid(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wad")));
            Assert.False(InstallValidation.IsFileInstallValid(null));
        }

        [Fact]
        public void ResolveInstalledPath_FolderGame_PointsAtFolder()
        {
            var result = InstallValidation.ResolveInstalledPath("R:\\Roms", "PS3", "Drakengard 3", true);

            Assert.Equal(
                Path.Combine("R:\\Roms", "romm", "PS3", "Drakengard 3"),
                result);
        }

        [Fact]
        public void ResolveInstalledPath_FileGame_PrefersExistingFile()
        {
            var roms = Path.Combine(Path.GetTempPath(), "resolverom_" + Guid.NewGuid().ToString("N"));
            try
            {
                var sub = Path.Combine(roms, "romm");
                Directory.CreateDirectory(sub);
                var file = Path.Combine(sub, "game.wad");
                File.WriteAllText(file, "x");

                var result = InstallValidation.ResolveInstalledPath(roms, "", "game.wad", false);

                Assert.Equal(file, result);
            }
            finally
            {
                Directory.Delete(roms, true);
            }
        }

        [Fact]
        public void IsFolderGameEffective_FlagTrue_Wins()
        {
            Assert.True(InstallValidation.IsFolderGameEffective(true, "R:\\Roms", "PS3", "game.zip"));
        }

        [Fact]
        public void IsFolderGameEffective_ZipName_IsFile()
        {
            Assert.False(InstallValidation.IsFolderGameEffective(false, "R:\\Roms", "PS3", "game.zip"));
        }

        [Fact]
        public void IsFolderGameEffective_DiskEvidence_OverridesStaleFlag()
        {
            var roms = Path.Combine(Path.GetTempPath(), "foldereff_" + Guid.NewGuid().ToString("N"));
            try
            {
                var folder = Path.Combine(roms, "romm", "PS3", "Drakengard 3");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "_launchbox.json"), "{}");

                Assert.True(InstallValidation.IsFolderGameEffective(false, roms, "PS3", "Drakengard 3"));
            }
            finally
            {
                Directory.Delete(roms, true);
            }
        }

        [Fact]
        public void IsFolderGameEffective_MissingFolder_IsFile()
        {
            var roms = Path.Combine(Path.GetTempPath(), "foldereff_" + Guid.NewGuid().ToString("N"));

            Assert.False(InstallValidation.IsFolderGameEffective(false, roms, "PS3", "Drakengard 3"));
        }

        [Fact]
        public void InstallProcessLock_AcquireReleaseReacquire()
        {
            // Cross-thread/cross-process exclusion is enforced by the OS named
            // mutex (verified against a live probe); here we only assert the
            // acquire/release lifecycle this wrapper controls.
            Assert.True(InstallProcessLock.TryEnter());
            InstallProcessLock.Exit();

            Assert.True(InstallProcessLock.TryEnter());
            InstallProcessLock.Exit();

            // Double release must never throw.
            var exception = Record.Exception(() => InstallProcessLock.Exit());
            Assert.Null(exception);
        }
    }
}
