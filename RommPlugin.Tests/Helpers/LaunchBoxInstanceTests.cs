using RommPlugin.Core.Helpers;
using Xunit;

namespace RommPlugin.Tests.Helpers
{
    public class LaunchBoxInstanceTests
    {
        [Fact]
        public void IsRecoveryInstance_DetectsTheSwitch()
        {
            Assert.True(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", "-recovery" }));
        }

        [Fact]
        public void IsRecoveryInstance_IsCaseInsensitive()
        {
            Assert.True(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", "-RECOVERY" }));
            Assert.True(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", "-Recovery" }));
        }

        [Fact]
        public void IsRecoveryInstance_ToleratesQuotesAndSpaces()
        {
            Assert.True(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", " \"-recovery\" " }));
        }

        [Fact]
        public void IsRecoveryInstance_PrimaryInstanceIsNotRecovery()
        {
            Assert.False(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe" }));
            Assert.False(LaunchBoxInstance.IsRecoveryInstance(new string[0]));
            Assert.False(LaunchBoxInstance.IsRecoveryInstance(null));
        }

        [Fact]
        public void IsRecoveryInstance_DoesNotMatchSimilarArguments()
        {
            Assert.False(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", "-recovery2" }));
            Assert.False(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", "--recovery" }));
            Assert.False(LaunchBoxInstance.IsRecoveryInstance(new[] { "LaunchBox.exe", "-not-recovery" }));
        }
    }
}
