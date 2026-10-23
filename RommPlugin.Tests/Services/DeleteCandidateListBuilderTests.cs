using System;
using System.Collections.Generic;
using RommPlugin.Core.Decisions;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class DeleteCandidateListBuilderTests
    {
        private const string Installed = "R:\\Roms\\romm\\PS3\\Game";
        private const string Roms = "R:\\Roms";
        private const string LocalFile = "R:\\Roms\\romm\\PS3\\Game.zip";
        private const string LocalWithoutZip = "R:\\Roms\\romm\\PS3\\Game";

        private static Func<string, bool> ExistsIn(params string[] paths)
        {
            var set = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            return set.Contains;
        }

        [Fact]
        public void Builds_InstalledPathThenConstructed_WithoutConditionalProbes()
        {
            var groups = DeleteCandidateListBuilder.Build(
                Installed, Roms, "PS3", "Game.zip", ExistsIn());

            Assert.Equal(2, groups.Count);

            Assert.Equal("InstalledPath", groups[0].Label);
            Assert.Equal(Installed, groups[0].DescribedPath);
            Assert.Equal(new[] { Installed }, groups[0].Paths);

            Assert.Equal("constructed path", groups[1].Label);
            Assert.Equal(LocalFile, groups[1].DescribedPath);
            Assert.Equal(new[] { LocalFile }, groups[1].Paths);
        }

        [Fact]
        public void ConditionalProbes_FollowExistence_KeptInOrder()
        {
            var groups = DeleteCandidateListBuilder.Build(
                Installed, Roms, "PS3", "Game.zip",
                ExistsIn(LocalFile + ".zip", LocalWithoutZip));

            Assert.Equal(
                new[] { LocalFile, LocalFile + ".zip", LocalWithoutZip },
                groups[1].Paths);
        }

        [Fact]
        public void OnlyZipVariant_WhenWithoutZipMissing()
        {
            var groups = DeleteCandidateListBuilder.Build(
                null, Roms, "PS3", "Game.zip", ExistsIn(LocalFile + ".zip"));

            Assert.Single(groups);
            Assert.Equal("constructed path", groups[0].Label);
            Assert.Equal(new[] { LocalFile, LocalFile + ".zip" }, groups[0].Paths);
        }

        [Fact]
        public void NonZipName_NeverProbesWithoutZip()
        {
            var groups = DeleteCandidateListBuilder.Build(
                null, Roms, "PS3", "Game.iso", ExistsIn("R:\\Roms\\romm\\PS3\\Game.iso"));

            Assert.Single(groups);
            Assert.Equal(new[] { "R:\\Roms\\romm\\PS3\\Game.iso" }, groups[0].Paths);
        }

        [Fact]
        public void InstalledPathOnly_WhenRemoteOrFileMissing()
        {
            var groups = DeleteCandidateListBuilder.Build(Installed, Roms, null, null, ExistsIn());

            Assert.Single(groups);
            Assert.Equal("InstalledPath", groups[0].Label);
            Assert.Equal(new[] { Installed }, groups[0].Paths);
        }

        [Fact]
        public void EmptyInstalledAndNoRemote_YieldsNothing()
        {
            var groups = DeleteCandidateListBuilder.Build(null, Roms, null, null, ExistsIn());

            Assert.Empty(groups);
        }
    }
}
