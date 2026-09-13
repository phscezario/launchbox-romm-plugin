using System.Linq;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class UpdateReconcileTests
    {
        private static readonly string[] Package =
        {
            "RommPlugin.dll",
            "RommPlugin.Core.dll",
            "Images\\ico.png",
            "Images\\ico.ico",
            "Locales\\en.json"
        };

        [Fact]
        public void ComputeObsoleteFiles_FlagsRemovedAssets()
        {
            var deployed = Package.Concat(new[]
            {
                "Images\\Installed.png",
                "Images\\Installed Games.png",
                "Locales\\old-lang.json"
            });

            var obsolete = UpdateReconcile.ComputeObsoleteFiles(deployed, Package);

            Assert.Equal(3, obsolete.Count);
            Assert.Contains("Images\\Installed.png", obsolete);
            Assert.Contains("Images\\Installed Games.png", obsolete);
            Assert.Contains("Locales\\old-lang.json", obsolete);
        }

        [Fact]
        public void ComputeObsoleteFiles_KeepsRuntimeData()
        {
            var deployed = Package.Concat(new[]
            {
                "settings.json",
                "download-state.json",
                "installed-games.json",
                "Logs\\romm-2026-09-08.log",
                "Logs\\cli-2026-09-08.log"
            });

            var obsolete = UpdateReconcile.ComputeObsoleteFiles(deployed, Package);

            Assert.Empty(obsolete);
        }

        [Fact]
        public void ComputeObsoleteFiles_IsCaseInsensitiveAndSeparatorAgnostic()
        {
            var obsolete = UpdateReconcile.ComputeObsoleteFiles(
                new[] { "images/INSTALLED.PNG", "SETTINGS.JSON" },
                new[] { "Images\\ico.png" });

            Assert.Single(obsolete);
            Assert.Equal("images\\INSTALLED.PNG", obsolete[0]);
        }

        [Fact]
        public void ComputeObsoleteFiles_EmptyInputs_YieldEmpty()
        {
            Assert.Empty(UpdateReconcile.ComputeObsoleteFiles(null, null));
            Assert.Empty(UpdateReconcile.ComputeObsoleteFiles(Package, Package));
        }
    }
}
