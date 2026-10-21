using System.Collections.Generic;
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
            _gateway.Verify(g => g.ClearAdditionalApplications(7, "C:\\g"), Times.Once);
            _gateway.Verify(g => g.DetachForUninstall(7), Times.Once);
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
            _gateway.Verify(g => g.ClearAdditionalApplications(7, "C:\\g"), Times.Once);
            _gateway.Verify(g => g.DetachForUninstall(7), Times.Once);
            _installed.Verify(s => s.MarkUninstalled(7), Times.Once);
            _queue.Verify(q => q.Remove(7), Times.Once);
            _deleter.Verify(d => d.CleanAfterUninstall(record, "R"), Times.Once);
        }

        [Fact]
        public void Failed_TouchesNothing_SoTheUserCanRetry()
        {
            var record = Record();
            _deleter.Setup(d => d.DeleteRecordPaths(record, "R")).Returns(GamePathDeleteOutcome.Failed);

            var outcome = _uninstaller.Uninstall(record, "R");

            Assert.Equal(GamePathDeleteOutcome.Failed, outcome);
            // Additional applications are cleared before the delete (they are
            // only identifiable while the folder config still exists), but a
            // failed delete must not detach the LaunchBox entry nor drop the
            // record/queue item: the game would be half-removed (files on disk,
            // nothing left in the UI to retry from).
            _gateway.Verify(g => g.ClearAdditionalApplications(7, "C:\\g"), Times.Once);
            _gateway.Verify(g => g.DetachForUninstall(It.IsAny<int>()), Times.Never);
            _installed.Verify(s => s.MarkUninstalled(It.IsAny<int>()), Times.Never);
            _queue.Verify(q => q.Remove(It.IsAny<int>()), Times.Never);
            _deleter.Verify(d => d.CleanAfterUninstall(It.IsAny<InstalledGameRecord>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ClearsAdditionalApplications_BeforeDeleting_AndDetachesOnlyAfter()
        {
            // The additional-application list lives in a config file inside the
            // installed folder, so it can only be read before the delete; the
            // LaunchBox entry itself must only be detached afterwards.
            var record = Record();
            var order = new List<string>();

            _gateway.Setup(g => g.ClearAdditionalApplications(7, "C:\\g"))
                .Callback(() => order.Add("clear-apps"));
            _deleter.Setup(d => d.DeleteRecordPaths(record, "R"))
                .Callback(() => order.Add("delete"))
                .Returns(GamePathDeleteOutcome.Deleted);
            _gateway.Setup(g => g.DetachForUninstall(7))
                .Callback(() => order.Add("detach"));

            _uninstaller.Uninstall(record, "R");

            Assert.Equal(new[] { "clear-apps", "delete", "detach" }, order);
        }

        [Fact]
        public void NullRecord_IsAlreadyGone_AndTouchesNothing()
        {
            Assert.Equal(GamePathDeleteOutcome.AlreadyGone, _uninstaller.Uninstall(null, "R"));
            _gateway.Verify(g => g.ClearAdditionalApplications(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _gateway.Verify(g => g.DetachForUninstall(It.IsAny<int>()), Times.Never);
        }
    }
}
