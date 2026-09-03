using System.Collections.Generic;
using Newtonsoft.Json;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    public class RommScreenshotTests
    {
        [Fact]
        public void RommScreenshot_SerializesCorrectly()
        {
            var screenshot = new RommScreenshot
            {
                Id = 1,
                FileName = "screenshot1.png",
                FileNameNoExt = "screenshot1",
                FileSizeBytes = 102400,
                IsGallery = true,
                IsPublic = false
            };

            var json = JsonConvert.SerializeObject(screenshot);

            Assert.Contains("\"id\":1", json);
            Assert.Contains("\"file_name\":\"screenshot1.png\"", json);
            Assert.Contains("\"file_name_no_ext\":\"screenshot1\"", json);
            Assert.Contains("\"file_size_bytes\":102400", json);
            Assert.Contains("\"is_gallery\":true", json);
            Assert.Contains("\"is_public\":false", json);
        }

        [Fact]
        public void RommScreenshot_DeserializesCorrectly()
        {
            var json = "{\"id\":2,\"file_name\":\"shot.jpg\",\"file_name_no_ext\":\"shot\",\"file_size_bytes\":204800,\"is_gallery\":false,\"is_public\":true}";
            var screenshot = JsonConvert.DeserializeObject<RommScreenshot>(json);

            Assert.NotNull(screenshot);
            Assert.Equal(2, screenshot.Id);
            Assert.Equal("shot.jpg", screenshot.FileName);
            Assert.Equal("shot", screenshot.FileNameNoExt);
            Assert.Equal(204800, screenshot.FileSizeBytes);
            Assert.False(screenshot.IsGallery);
            Assert.True(screenshot.IsPublic);
        }

        [Fact]
        public void RommGame_UserScreenshots_DefaultIsEmpty()
        {
            var game = new RommGame();
            Assert.NotNull(game.UserScreenshots);
            Assert.Empty(game.UserScreenshots);
        }

        [Fact]
        public void RommGame_UserScreenshots_SerializesCorrectly()
        {
            var game = new RommGame
            {
                Name = "Test Game",
                UserScreenshots = new List<RommScreenshot>
                {
                    new RommScreenshot
                    {
                        Id = 1,
                        FileName = "shot1.png",
                        FileNameNoExt = "shot1",
                        FileSizeBytes = 1024
                    },
                    new RommScreenshot
                    {
                        Id = 2,
                        FileName = "shot2.jpg",
                        FileNameNoExt = "shot2",
                        FileSizeBytes = 2048
                    }
                }
            };

            var json = JsonConvert.SerializeObject(game);
            var deserialized = JsonConvert.DeserializeObject<RommGame>(json);

            Assert.NotNull(deserialized.UserScreenshots);
            Assert.Equal(2, deserialized.UserScreenshots.Count);
            Assert.Equal(1, deserialized.UserScreenshots[0].Id);
            Assert.Equal("shot1.png", deserialized.UserScreenshots[0].FileName);
            Assert.Equal(2, deserialized.UserScreenshots[1].Id);
            Assert.Equal("shot2.jpg", deserialized.UserScreenshots[1].FileName);
        }

        [Fact]
        public void RommGame_UserScreenshots_DeserializesWhenMissing()
        {
            var json = "{\"name\":\"Test Game\"}";
            var game = JsonConvert.DeserializeObject<RommGame>(json);

            Assert.NotNull(game);
            Assert.NotNull(game.UserScreenshots);
            Assert.Empty(game.UserScreenshots);
        }
    }
}
