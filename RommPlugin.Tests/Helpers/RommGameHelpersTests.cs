using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using RommPlugin.Core.Models;
using RommPlugin.Helpers;
using Unbroken.LaunchBox.Plugins.Data;
using Xunit;

namespace RommPlugin.Tests.Helpers
{
    public class RommGameHelpersTests
    {
        private static Mock<IGame> CreateGameWithCustomField(string name, string value)
        {
            var field = new Mock<ICustomField>();
            field.SetupProperty(f => f.Name, name);
            field.SetupProperty(f => f.Value, value);

            var fields = new List<ICustomField> { field.Object };

            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(fields.ToArray());

            return game;
        }

        private static Mock<IGame> CreateGameWithCustomFields(params (string name, string value)[] fields)
        {
            var customFields = new List<ICustomField>();

            foreach (var (name, value) in fields)
            {
                var field = new Mock<ICustomField>();
                field.SetupProperty(f => f.Name, name);
                field.SetupProperty(f => f.Value, value);
                customFields.Add(field.Object);
            }

            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(customFields.ToArray());

            return game;
        }

        private static Mock<IGame> CreateGameWithNewCustomField()
        {
            var newField = new Mock<ICustomField>();
            newField.SetupProperty(f => f.Name, "");
            newField.SetupProperty(f => f.Value, "");

            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(Array.Empty<ICustomField>());
            game.Setup(g => g.AddNewCustomField()).Returns(newField.Object);

            return game;
        }

        private static Mock<ICustomField> CreateField(string name, string value)
        {
            var field = new Mock<ICustomField>();
            field.SetupProperty(f => f.Name, name);
            field.SetupProperty(f => f.Value, value);
            return field;
        }

        [Fact]
        public void TryGetRommId_ReturnsTrue_WhenFieldExists()
        {
            var game = CreateGameWithCustomField(GameCustomFields.GameId, "42");

            var result = RommGameHelpers.TryGetRommId(game.Object, out var rommId);

            Assert.True(result);
            Assert.Equal(42, rommId);
        }

        [Fact]
        public void TryGetRommId_ReturnsFalse_WhenFieldMissing()
        {
            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(Array.Empty<ICustomField>());

            var result = RommGameHelpers.TryGetRommId(game.Object, out var rommId);

            Assert.False(result);
            Assert.Equal(0, rommId);
        }

        [Fact]
        public void TryGetRommId_ReturnsFalse_WhenNotNumeric()
        {
            var game = CreateGameWithCustomField(GameCustomFields.GameId, "abc");

            var result = RommGameHelpers.TryGetRommId(game.Object, out var rommId);

            Assert.False(result);
            Assert.Equal(0, rommId);
        }

        [Fact]
        public void GetRommId_ReturnsId_WhenFieldExists()
        {
            var game = CreateGameWithCustomField(GameCustomFields.GameId, "99");

            var result = RommGameHelpers.GetRommId(game.Object);

            Assert.Equal(99, result);
        }

        [Fact]
        public void GetRommId_ReturnsZero_WhenFieldMissing()
        {
            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(Array.Empty<ICustomField>());

            var result = RommGameHelpers.GetRommId(game.Object);

            Assert.Equal(0, result);
        }

        [Fact]
        public void SetCustomField_CreatesNewField_WhenNotExists()
        {
            var newField = CreateField("", "");
            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(Array.Empty<ICustomField>());
            game.Setup(g => g.AddNewCustomField()).Returns(newField.Object);

            RommGameHelpers.SetCustomField(game.Object, "test_field", "test_value");

            Assert.Equal("test_field", newField.Object.Name);
            Assert.Equal("test_value", newField.Object.Value);
        }

        [Fact]
        public void SetCustomField_UpdatesExistingField()
        {
            var field = CreateField("test_field", "old_value");
            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(new[] { field.Object });

            RommGameHelpers.SetCustomField(game.Object, "test_field", "new_value");

            Assert.Equal("new_value", field.Object.Value);
        }

        [Fact]
        public void SetCustomField_PreservesOldValue_WhenOverwriteFalse()
        {
            var field = CreateField("test_field", "old_value");
            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(new[] { field.Object });

            RommGameHelpers.SetCustomField(game.Object, "test_field", "new_value", overwrite: false);

            Assert.Equal("old_value", field.Object.Value);
        }

        [Fact]
        public void GetCustomField_ReturnsValue_WhenFieldExists()
        {
            var game = CreateGameWithCustomField("test_field", "test_value");

            var result = RommGameHelpers.GetCustomField(game.Object, "test_field");

            Assert.Equal("test_value", result);
        }

        [Fact]
        public void GetCustomField_ReturnsNull_WhenGameIsNull()
        {
            var result = RommGameHelpers.GetCustomField(null, "test_field");

            Assert.Null(result);
        }

        [Fact]
        public void GetCustomField_ReturnsNull_WhenFieldMissing()
        {
            var game = new Mock<IGame>();
            game.Setup(g => g.GetAllCustomFields()).Returns(Array.Empty<ICustomField>());

            var result = RommGameHelpers.GetCustomField(game.Object, "test_field");

            Assert.Null(result);
        }

        [Theory]
        [InlineData("Game.zip", "Game")]
        [InlineData("Game.bin", "Game")]
        [InlineData("Game.iso", "Game")]
        [InlineData("Game.7z", "Game")]
        [InlineData("Game.nes", "Game")]
        [InlineData("Game.n64", "Game")]
        public void NormalizeGameTitle_RemovesKnownExtension(string input, string expected)
        {
            var result = RommGameHelpers.NormalizeGameTitle(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeGameTitle_PreservesUnknownExtension()
        {
            var result = RommGameHelpers.NormalizeGameTitle("Game.exe");

            Assert.Equal("Game.exe", result);
        }

        [Fact]
        public void NormalizeGameTitle_ReturnsNull_WhenInputNull()
        {
            var result = RommGameHelpers.NormalizeGameTitle(null);

            Assert.Null(result);
        }

        [Fact]
        public void NormalizeGameTitle_ReturnsEmpty_WhenInputEmpty()
        {
            var result = RommGameHelpers.NormalizeGameTitle("");

            Assert.Equal("", result);
        }

        [Fact]
        public void NormalizeGameTitle_RemovesMultipleKnownExtensions()
        {
            var result = RommGameHelpers.NormalizeGameTitle("Game.bin.zip");

            Assert.Equal("Game", result);
        }

        [Theory]
        [InlineData("Arcade", "Arcade")]
        [InlineData("Console", "Consoles")]
        [InlineData("Operating System", "Computers")]
        [InlineData("Portable Console", "Handhelds")]
        [InlineData("Unknown", "Others")]
        [InlineData("Other Value", "Others")]
        public void ParseCategory_ReturnsCorrectCategory(string input, string expected)
        {
            var result = RommGameHelpers.ParseCategory(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void SanitizeFolderName_RemovesInvalidChars()
        {
            var result = RommGameHelpers.SanitizeFolderName("Game<>:\"/\\|?*Name");

            Assert.Equal("GameName", result);
        }

        [Fact]
        public void SanitizeFolderName_PreservesValidName()
        {
            var result = RommGameHelpers.SanitizeFolderName("Game Name");

            Assert.Equal("Game Name", result);
        }

        [Fact]
        public void SanitizeFolderName_TrimSpaces()
        {
            var result = RommGameHelpers.SanitizeFolderName("  Game Name  ");

            Assert.Equal("Game Name", result);
        }

        [Fact]
        public void EnsureDirectoryExists_CreatesDirectory_WhenMissing()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "romm_test_" + Path.GetRandomFileName());
            var filePath = Path.Combine(tempDir, "sub", "file.txt");

            try
            {
                RommGameHelpers.EnsureDirectoryExists(filePath);

                // Asserting only tempDir passed even when the sub folder the file
                // actually needs was never created.
                Assert.True(Directory.Exists(Path.Combine(tempDir, "sub")));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ResolvePath_ReturnsAbsolutePath_WhenFromLaunchBoxRoot()
        {
            var result = RommGameHelpers.ResolvePath("/base", "/absolute/path/file.exe", fromLaunchBoxRoot: true);

            Assert.Equal("/absolute/path/file.exe", result);
        }

        [Fact]
        public void ResolvePath_ResolvesRelativePath()
        {
            var result = RommGameHelpers.ResolvePath("/base", "relative/path/file.exe", fromLaunchBoxRoot: false);

            // Asserting Contains("relative") also passed when the input was
            // returned untouched, which is the case under test.
            var expected = Path.GetFullPath(Path.Combine("/base", "relative/path/file.exe"));
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ResolvePath_ReturnsNull_WhenPathNull()
        {
            var result = RommGameHelpers.ResolvePath("/base", null, fromLaunchBoxRoot: false);

            Assert.Null(result);
        }

        private static Mock<IAdditionalApplication> MockApp(string name, string applicationPath)
        {
            var app = new Mock<IAdditionalApplication>();
            app.SetupProperty(a => a.Name, name);
            app.SetupProperty(a => a.ApplicationPath, applicationPath);
            return app;
        }

        private static string CreateFolderGameWithJson(string json)
        {
            var root = Path.Combine(Path.GetTempPath(), "dlcuninstall_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "_launchbox.json"), json);
            return root;
        }

        [Fact]
        public void ClearGameAdditionalApplications_RemovesDlcEntries_WhenHasDlc()
        {
            var root = CreateFolderGameWithJson("{\"DefaultFileName\": \"\", \"HasDLC\": true, \"AdditionalApplications\": []}");
            try
            {
                var dlcApp = MockApp("DLC 1", Path.Combine(root, "_DLCs", "a.pkg"));
                var otherApp = MockApp("RomM | Other", Path.Combine(root, "other.exe"));
                var game = new Mock<IGame>();
                game.Setup(g => g.GetAllAdditionalApplications()).Returns(new[] { dlcApp.Object, otherApp.Object });

                RommGameHelpers.ClearGameAdditionalApplications(game.Object, root);

                game.Verify(g => g.TryRemoveAdditionalApplication(dlcApp.Object), Times.Once);
                game.Verify(g => g.TryRemoveAdditionalApplication(otherApp.Object), Times.Never);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Fact]
        public void ClearGameAdditionalApplications_KeepsDlcEntries_WhenHasDlcFalse()
        {
            var root = CreateFolderGameWithJson("{\"DefaultFileName\": \"\", \"HasDLC\": false, \"AdditionalApplications\": []}");
            try
            {
                var dlcApp = MockApp("DLC 1", Path.Combine(root, "_DLCs", "a.pkg"));
                var game = new Mock<IGame>();
                game.Setup(g => g.GetAllAdditionalApplications()).Returns(new[] { dlcApp.Object });

                RommGameHelpers.ClearGameAdditionalApplications(game.Object, root);

                game.Verify(g => g.TryRemoveAdditionalApplication(dlcApp.Object), Times.Never);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Fact]
        public void ClearGameAdditionalApplications_RemovesJsonListedEntries()
        {
            var root = CreateFolderGameWithJson("{\"DefaultFileName\": \"\", \"HasDLC\": false, \"AdditionalApplications\": [{\"Name\": \"Tool\", \"Path\": \"tool.exe\"}]}");
            try
            {
                var toolApp = MockApp("RomM | Tool", Path.Combine(root, "tool.exe"));
                var game = new Mock<IGame>();
                game.Setup(g => g.GetAllAdditionalApplications()).Returns(new[] { toolApp.Object });

                RommGameHelpers.ClearGameAdditionalApplications(game.Object, root);

                game.Verify(g => g.TryRemoveAdditionalApplication(toolApp.Object), Times.Once);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
    }
}
