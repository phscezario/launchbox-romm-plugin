using RommPlugin.Core.Decisions;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class DlcEntryRoutingTests
    {
        [Fact]
        public void OptIn_WithEmulator_RoutesViaEmulator()
        {
            var route = DlcEntryRouting.Decide(true, "rpcs3-id");

            Assert.True(route.UseEmulator);
            Assert.Equal("rpcs3-id", route.EmulatorId);
            Assert.True(route.WaitForExit);
        }

        [Fact]
        public void OptIn_WithoutEmulator_FallsBackToDirect()
        {
            var route = DlcEntryRouting.Decide(true, null);

            Assert.False(route.UseEmulator);
            Assert.Null(route.EmulatorId);
            Assert.False(route.WaitForExit);
        }

        [Fact]
        public void OptOut_LaunchesDirectly()
        {
            foreach (var flag in new bool?[] { false, null })
            {
                var route = DlcEntryRouting.Decide(flag, "emu-id");

                Assert.False(route.UseEmulator);
                Assert.Null(route.EmulatorId);
                Assert.False(route.WaitForExit);
            }
        }
    }
}
