using System;
using System.IO;
using System.Threading;
using RommPlugin.Core.Decisions;
using RommPlugin.Core.Helpers;
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
            // semaphore (verified against a live probe); here we only assert the
            // acquire/release lifecycle this wrapper controls. A semaphore (not
            // a mutex) is used precisely because release is not thread-affine.
            Assert.True(InstallProcessLock.TryEnter());
            InstallProcessLock.Exit();

            Assert.True(InstallProcessLock.TryEnter());
            InstallProcessLock.Exit();
        }

        [Fact]
        public void InstallProcessLock_SecondThreadIsRefusedWhileHeld()
        {
            // Documents the single-flight contract the install service now
            // relies on: while one run holds the lock, another is refused.
            Assert.True(InstallProcessLock.TryEnter());
            try
            {
                var enteredElsewhere = true;
                var thread = new Thread(() =>
                {
                    enteredElsewhere = InstallProcessLock.TryEnter();
                    if (enteredElsewhere) InstallProcessLock.Exit();
                });
                thread.Start();
                thread.Join();

                Assert.False(enteredElsewhere);
            }
            finally
            {
                InstallProcessLock.Exit();
            }
        }
        [Fact]
        public void InstallProcessLock_WaitsForTheRunningPassInsteadOfSkipping()
        {
            // A trigger that arrives while another pass is extracting must
            // survive: the running pass snapshotted the state file before this
            // item appeared, so dropping the trigger left it on "Installing".
            Assert.True(InstallProcessLock.TryEnter());

            var acquired = false;

            try
            {
                var thread = new Thread(() =>
                {
                    acquired = InstallProcessLock.TryEnter(TimeSpan.FromSeconds(5));
                    if (acquired) InstallProcessLock.Exit();
                });
                thread.Start();

                Thread.Sleep(300);
                InstallProcessLock.Exit();
                thread.Join();
            }
            finally
            {
                InstallProcessLock.Exit();
            }

            // The contract is "waits for the token instead of being refused"; the
            // elapsed time depends on the scheduler and a 50 ms margin made this
            // test flaky on a loaded machine.
            Assert.True(acquired);
        }

        [Fact]
        public void InstallProcessLock_GivesUpAfterTheTimeout()
        {
            Assert.True(InstallProcessLock.TryEnter());
            try
            {
                var acquired = true;
                var thread = new Thread(() => acquired = InstallProcessLock.TryEnter(TimeSpan.FromMilliseconds(300)));
                thread.Start();
                thread.Join();

                Assert.False(acquired);
            }
            finally
            {
                InstallProcessLock.Exit();
            }
        }

        [Fact]
        public void InstallProcessLock_TakesOverFromADeadOwner()
        {
            // A semaphore is not abandoned when its owner dies, so the wrapper
            // records the owning pid and reclaims the lock once that process is
            // gone. Without this a crashed LaunchBox instance would lock every
            // later install out for the lifetime of the survivor.
            Assert.True(InstallProcessLock.TryEnter());
            try
            {
                var ownerFile = Path.Combine(RommPlugin.Core.Storage.RommPaths.PluginFolder, "install-processing.owner");
                File.WriteAllText(ownerFile, UnusedProcessId().ToString());

                var enteredElsewhere = false;
                var thread = new Thread(() => enteredElsewhere = InstallProcessLock.TryEnter());
                thread.Start();
                thread.Join();

                Assert.True(enteredElsewhere);
                InstallProcessLock.Exit();
            }
            finally
            {
                InstallProcessLock.Exit();
            }
        }

        [Fact]
        public void InstallProcessLock_ExitWithoutOwnershipDoesNotInflateTheSemaphore()
        {
            // A fail-open acquisition returns true without taking the semaphore.
            // Releasing it there raised the count and let a third process into
            // the extraction while the real owner was still running.
            InstallProcessLock.Exit();

            Assert.True(InstallProcessLock.TryEnter());
            try
            {
                Assert.False(InstallProcessLock.TryEnter());
            }
            finally
            {
                InstallProcessLock.Exit();
            }
        }

        private static int UnusedProcessId()
        {
            // Windows process ids are always multiples of 4, so an odd value in a
            // plausible range is reliably unused and GetProcessById throws
            // ArgumentException for it — exactly what the dead-owner check relies on.
            for (var pid = 60001; pid < 65535; pid += 2)
            {
                try
                {
                    using (System.Diagnostics.Process.GetProcessById(pid)) { }
                }
                catch (ArgumentException)
                {
                    return pid;
                }
            }

            throw new InvalidOperationException("No unused process id found");
        }
    }
}
