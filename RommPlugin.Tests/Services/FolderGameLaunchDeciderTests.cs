using System.IO;
using System.Linq;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class FolderGameLaunchDeciderTests
    {
        private static string BaseFolder => Path.GetFullPath("C:/Games/PS3/God of War 3");

        [Fact]
        public void EmptyDefaultFileName_PointsApplicationPathToFolder()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "" };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", null);

            Assert.Equal(Path.GetFullPath(BaseFolder), plan.ApplicationPath);
            Assert.Empty(plan.DlcEntries);
        }

        [Fact]
        public void NullDefaultFileName_PointsApplicationPathToFolder()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = null };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", null);

            Assert.Equal(Path.GetFullPath(BaseFolder), plan.ApplicationPath);
        }

        [Fact]
        public void SetDefaultFileName_CombinesWithFolder()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "shortcuts/Game.lnk" };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", null);

            Assert.Equal(Path.GetFullPath(Path.Combine(BaseFolder, "shortcuts/Game.lnk")), plan.ApplicationPath);
        }

        [Fact]
        public void NullConfig_ReturnsEmptyPlan()
        {
            var plan = FolderGameLaunchDecider.Decide(BaseFolder, null, "emu-id", new[] { "dlc.pkg" });

            Assert.Null(plan.ApplicationPath);
            Assert.Empty(plan.DlcEntries);
        }

        [Fact]
        public void HasDlcWithoutOptIn_EntriesLaunchDirectly()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "", HasDLC = true };
            var files = new[] { "b.pkg", "a.pkg" };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", files);

            Assert.Equal(2, plan.DlcEntries.Count);
            Assert.All(plan.DlcEntries, e =>
            {
                Assert.False(e.UseEmulator);
                Assert.Null(e.EmulatorId);
                Assert.False(e.WaitForExit);
            });
            // Stable order and numbering.
            Assert.Equal("b.pkg", Path.GetFileName(plan.DlcEntries[1].ApplicationPath));
            Assert.EndsWith("DLC 1", plan.DlcEntries[0].Name);
            Assert.EndsWith("DLC 2", plan.DlcEntries[1].Name);
        }

        [Fact]
        public void HasDlcWithOptIn_EntriesRouteViaGameEmulator()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "", HasDLC = true, DlcUseEmulator = true };
            var files = new[] { "dlc.pkg" };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "rpcs3-id", files);

            var entry = Assert.Single(plan.DlcEntries);
            Assert.True(entry.UseEmulator);
            Assert.Equal("rpcs3-id", entry.EmulatorId);
            Assert.True(entry.WaitForExit);
        }

        [Fact]
        public void DlcOptInWithoutEmulatorId_FallsBackToDirectLaunch()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "", HasDLC = true, DlcUseEmulator = true };
            var files = new[] { "dlc.pkg" };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, null, files);

            var entry = Assert.Single(plan.DlcEntries);
            Assert.False(entry.UseEmulator);
            Assert.Null(entry.EmulatorId);
        }

        [Fact]
        public void HasDlcFalse_YieldsNoEntries()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "", HasDLC = false, DlcUseEmulator = true };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", new[] { "dlc.pkg" });

            Assert.Empty(plan.DlcEntries);
        }

        [Fact]
        public void DlcOptIn_RoutesConfigAppsViaEmulator()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "", DlcUseEmulator = true };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", null);

            Assert.True(plan.RouteConfigAppsViaEmulator);
        }

        [Fact]
        public void DlcOptOut_ConfigAppsLaunchDirectly()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "" };

            var plan = FolderGameLaunchDecider.Decide(BaseFolder, config, "emu-id", null);

            Assert.False(plan.RouteConfigAppsViaEmulator);
        }

        [Fact]
        public void EmptyDefaultFileName_AllowsShortcutsCleanup()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "" };

            Assert.True(FolderGameLaunchDecider.ShouldRemoveShortcutsFolder(BaseFolder, config));
        }

        [Fact]
        public void SetDefaultFileName_KeepsShortcutsFolder()
        {
            var config = new LaunchBoxFolderGameConfig { DefaultFileName = "shortcuts/Game.lnk" };

            Assert.False(FolderGameLaunchDecider.ShouldRemoveShortcutsFolder(BaseFolder, config));
        }

        [Fact]
        public void ConfigEntryUnderShortcuts_KeepsShortcutsFolder()
        {
            var config = new LaunchBoxFolderGameConfig
            {
                DefaultFileName = "",
                AdditionalApplications = new System.Collections.Generic.List<AdditionalApplications>
                {
                    new AdditionalApplications { Name = "Tool", Path = "shortcuts/Tool.lnk" },
                },
            };

            Assert.False(FolderGameLaunchDecider.ShouldRemoveShortcutsFolder(BaseFolder, config));
        }

        [Fact]
        public void NullConfig_KeepsShortcutsFolder()
        {
            Assert.False(FolderGameLaunchDecider.ShouldRemoveShortcutsFolder(BaseFolder, null));
        }
    }
}
