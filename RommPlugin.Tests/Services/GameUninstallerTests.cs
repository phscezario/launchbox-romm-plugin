using Moq;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class GameUninstallerTests
    {
        private readonly Mock<ILaunchBoxGameGateway> _gateway = new Mock<ILaunchBoxGameGateway>();
        private readonly Mock<IGameFileDeleter> _deleter = new Mock<IGameFileDeleter>();
        private readonly Mock<IInstalledGamesService> _installed = new Mock<IInstalledGamesService>();
        private readonly Mock<IDownloadQueueService> _queue = new Mock<IDownloadQueueService>();
        private readonly GameUninstaller _uninstaller;

        public GameUninstallerTests()
        {
            _uninstaller = new GameUninstaller(
                _gateway.Object, _deleter.Object, _installed.Object, _queue.Object);
        }

        private static InstalledGameRecord Record()
        {
            return new InstalledGameRecord { RommGameId = 7, Title = "G", InstalledPath = "C:\\g" };
        }

        [Fact]
        public void Success_MarksUninstalled_RemovesQueue_AndCleans()
        {
            var record = Record();
            _deleter.Setup(d => d.DeleteRecordPaths(record, "R")).Returns(GamePathDeleteOutcome.Deleted);

            var outcome = _uninstaller.Uninstall(record, "R");

            Assert.Equal(GamePathDeleteOutcome.Deleted, outcome);
            _gateway.Verify(g => g.ClearForUninstall(7, "C:\\g"), Times.Once);
            _installed.Verify(s => s.MarkUninstalled(7), Times.Once);
            _queue.Verify(q => q.Remove(7), Times.Once);
            _deleter.Verify(d => d.CleanAfterUninstall(record, "R"), Times.Once);
        }

        [Fact]
        public void AlreadyGone_StillMarksUninstalled()
        {
            var record = Record();
            _deleter.Setup(d => d.DeleteRecordPaths(record, "R")).Returns(GamePathDeleteOutcome.AlreadyGone);

            var outcome = _uninstaller.Uninstall(record, "R");

            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, outcome);
            _installed.Verify(s => s.MarkUninstalled(7), Times.Once);
            _queue.Verify(q => q.Remove(7), Times.Once);
            _deleter.Verify(d => d.CleanAfterUninstall(record, "R"), Times.Once);
        }

        [Fact]
        public void Failed_ClearsLaunchBox_ButLeavesRecordAndQueueUntouched()
        {
            var record = Record();
            _deleter.Setup(d => d.DeleteRecordPaths(record, "R")).Returns(GamePathDeleteOutcome.Failed);

            var outcome = _uninstaller.Uninstall(record, "R");

            Assert.Equal(GamePathDeleteOutcome.Failed, outcome);
            // The LaunchBox entry is detached first, exactly like the Game Manager.
            _gateway.Verify(g => g.ClearForUninstall(7, "C:\\g"), Times.Once);
            _installed.Verify(s => s.MarkUninstalled(It.IsAny<int>()), Times.Never);
            _queue.Verify(q => q.Remove(It.IsAny<int>()), Times.Never);
            _deleter.Verify(d => d.CleanAfterUninstall(It.IsAny<InstalledGameRecord>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void NullRecord_IsAlreadyGone_AndTouchesNothing()
        {
            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, _uninstaller.Uninstall(null, "R"));
            _gateway.Verify(g => g.ClearForUninstall(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
}
