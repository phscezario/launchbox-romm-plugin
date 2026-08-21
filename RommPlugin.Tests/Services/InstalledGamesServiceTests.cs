using System;
using System.IO;
using System.Linq;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class InstalledGamesServiceTests : IDisposable
    {
        private readonly string _tempFile;

        public InstalledGamesServiceTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), "installed_test_" + Guid.NewGuid().ToString("N") + ".json");
        }

        public void Dispose()
        {
            try { File.Delete(_tempFile); } catch { }
        }

        [Fact]
        public void GetAll_ReturnsEmpty_WhenNoFile()
        {
            var service = new InstalledGamesService(_tempFile);
            var result = service.GetAll();
            Assert.Empty(result);
        }

        [Fact]
        public void MarkInstalled_AddsNewRecord()
        {
            var service = new InstalledGamesService(_tempFile);

            service.MarkInstalled(new InstalledGameRecord
            {
                RommGameId = 1,
                Title = "Test Game",
                Platform = "NES",
                FileName = "test.nes"
            });

            var all = service.GetAll();
            Assert.Single(all);
            Assert.Equal(1, all[0].RommGameId);
            Assert.Equal("Test Game", all[0].Title);
        }

        [Fact]
        public void MarkInstalled_UpdatesExistingRecord()
        {
            var service = new InstalledGamesService(_tempFile);

            service.MarkInstalled(new InstalledGameRecord
            {
                RommGameId = 1,
                Title = "Old Title",
                Platform = "NES"
            });

            service.MarkInstalled(new InstalledGameRecord
            {
                RommGameId = 1,
                Title = "New Title",
                Platform = "SNES"
            });

            var all = service.GetAll();
            Assert.Single(all);
            Assert.Equal("New Title", all[0].Title);
            Assert.Equal("SNES", all[0].Platform);
        }

        [Fact]
        public void MarkUninstalled_RemovesRecord()
        {
            var service = new InstalledGamesService(_tempFile);

            service.MarkInstalled(new InstalledGameRecord { RommGameId = 1, Title = "Game 1" });
            service.MarkInstalled(new InstalledGameRecord { RommGameId = 2, Title = "Game 2" });

            service.MarkUninstalled(1);
            service.RemoveUninstalled();

            var all = service.GetAll();
            Assert.Single(all);
            Assert.Equal(2, all[0].RommGameId);
        }

        [Fact]
        public void GetByGameId_ReturnsCorrectRecord()
        {
            var service = new InstalledGamesService(_tempFile);

            service.MarkInstalled(new InstalledGameRecord { RommGameId = 1, Title = "Game 1" });
            service.MarkInstalled(new InstalledGameRecord { RommGameId = 2, Title = "Game 2" });

            var result = service.GetByGameId(2);
            Assert.NotNull(result);
            Assert.Equal("Game 2", result.Title);
        }

        [Fact]
        public void GetByGameId_ReturnsNull_WhenNotFound()
        {
            var service = new InstalledGamesService(_tempFile);
            var result = service.GetByGameId(999);
            Assert.Null(result);
        }

        [Fact]
        public void IsInstalled_ReturnsTrue_WhenExists()
        {
            var service = new InstalledGamesService(_tempFile);
            service.MarkInstalled(new InstalledGameRecord { RommGameId = 1, Title = "Game" });
            Assert.True(service.IsInstalled(1));
        }

        [Fact]
        public void IsInstalled_ReturnsFalse_WhenNotExists()
        {
            var service = new InstalledGamesService(_tempFile);
            Assert.False(service.IsInstalled(1));
        }

        [Fact]
        public void PersistsToDisk()
        {
            var service = new InstalledGamesService(_tempFile);
            service.MarkInstalled(new InstalledGameRecord { RommGameId = 1, Title = "Persistent Game" });

            var service2 = new InstalledGamesService(_tempFile);
            var all = service2.GetAll();
            Assert.Single(all);
            Assert.Equal("Persistent Game", all[0].Title);
        }
    }
}
