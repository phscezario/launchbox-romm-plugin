using RommPlugin.Core.Decisions;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class InstallFailureCleanupTests
    {
        [Fact]
        public void ShouldDeleteExtractDir_True_WhenAttemptCreatedTheFolder()
        {
            Assert.True(InstallFailureCleanup.ShouldDeleteExtractDir(
                extractDirExistedBeforeAttempt: false,
                attemptPublishedExtractDir: true));
        }

        [Fact]
        public void ShouldDeleteExtractDir_False_WhenFolderExistedBefore()
        {
            Assert.False(InstallFailureCleanup.ShouldDeleteExtractDir(true, true));
            Assert.False(InstallFailureCleanup.ShouldDeleteExtractDir(true, false));
        }

        [Fact]
        public void ShouldDeleteExtractDir_False_WhenAttemptDidNotPublishIt()
        {
            // The exact race that produced ghost installs: the folder was absent
            // when the attempt started but another run created it, so the move
            // failed and this attempt never owned the folder.
            Assert.False(InstallFailureCleanup.ShouldDeleteExtractDir(
                extractDirExistedBeforeAttempt: false,
                attemptPublishedExtractDir: false));
        }
    }
}
