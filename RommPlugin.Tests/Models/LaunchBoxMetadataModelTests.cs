using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    public class LaunchBoxMetadataModelTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var model = new LaunchBoxMetadataModel();
            Assert.Null(model.FirstReleaseDate);
            Assert.Null(model.MaxPlayers);
            Assert.Null(model.ReleaseType);
            Assert.Null(model.Cooperative);
            Assert.Null(model.YoutubeVideoId);
            Assert.Equal(0f, model.CommunityRating);
            Assert.Equal(0, model.CommunityRatingCount);
            Assert.Null(model.WikipediaUrl);
            Assert.Null(model.Esrb);
            Assert.NotNull(model.Genres);
            Assert.Empty(model.Genres);
            Assert.NotNull(model.Companies);
            Assert.Empty(model.Companies);
            Assert.NotNull(model.Images);
            Assert.Empty(model.Images);
        }

        [Fact]
        public void Serialize_JsonContainsAllFields()
        {
            var model = new LaunchBoxMetadataModel
            {
                FirstReleaseDate = 1700000000,
                MaxPlayers = 4,
                ReleaseType = "Released",
                Cooperative = true,
                YoutubeVideoId = "dQw4w9WgXcQ",
                CommunityRating = 4.5f,
                CommunityRatingCount = 100,
                WikipediaUrl = "https://en.wikipedia.org/wiki/Test",
                Esrb = "E",
                Genres = new List<string> { "Action", "Adventure" },
                Companies = new List<string> { "Test Studio" },
                Images = new List<LaunchBoxImage>
                {
                    new LaunchBoxImage
                    {
                        Url = "https://example.com/image.png",
                        Type = "Clear Logo",
                        Region = ""
                    }
                }
            };

            var json = JsonConvert.SerializeObject(model);
            var deserialized = JsonConvert.DeserializeObject<LaunchBoxMetadataModel>(json);

            Assert.Equal(1700000000, deserialized.FirstReleaseDate);
            Assert.Equal(4, deserialized.MaxPlayers);
            Assert.Equal("Released", deserialized.ReleaseType);
            Assert.True(deserialized.Cooperative);
            Assert.Equal("dQw4w9WgXcQ", deserialized.YoutubeVideoId);
            Assert.Equal(4.5f, deserialized.CommunityRating);
            Assert.Equal(100, deserialized.CommunityRatingCount);
            Assert.Equal("https://en.wikipedia.org/wiki/Test", deserialized.WikipediaUrl);
            Assert.Equal("E", deserialized.Esrb);
            Assert.Equal(2, deserialized.Genres.Count);
            Assert.Contains("Action", deserialized.Genres);
            Assert.Contains("Adventure", deserialized.Genres);
            Assert.Single(deserialized.Companies);
            Assert.Equal("Test Studio", deserialized.Companies[0]);
            Assert.Single(deserialized.Images);
            Assert.Equal("Clear Logo", deserialized.Images[0].Type);
        }

        [Fact]
        public void Deserialize_HandlesMissingFields()
        {
            var json = "{}";
            var model = JsonConvert.DeserializeObject<LaunchBoxMetadataModel>(json);
            Assert.NotNull(model);
            Assert.Null(model.FirstReleaseDate);
            Assert.Null(model.MaxPlayers);
            Assert.NotNull(model.Genres);
            Assert.Empty(model.Genres);
            Assert.NotNull(model.Images);
            Assert.Empty(model.Images);
        }

        [Fact]
        public void JsonProperty_Attributes_AreRespected()
        {
            var json = @"{
                ""first_release_date"": 1700000000,
                ""max_players"": 2,
                ""release_type"": ""Beta"",
                ""cooperative"": false,
                ""youtube_video_id"": ""test123"",
                ""community_rating"": 3.5,
                ""community_rating_count"": 50,
                ""wikipedia_url"": ""https://example.com"",
                ""esrb"": ""T"",
                ""genres"": [""RPG""],
                ""companies"": [""Dev Studio""],
                ""images"": []
            }";

            var model = JsonConvert.DeserializeObject<LaunchBoxMetadataModel>(json);

            Assert.Equal(1700000000, model.FirstReleaseDate);
            Assert.Equal(2, model.MaxPlayers);
            Assert.Equal("Beta", model.ReleaseType);
            Assert.False(model.Cooperative);
            Assert.Equal("test123", model.YoutubeVideoId);
            Assert.Equal(3.5f, model.CommunityRating);
            Assert.Equal(50, model.CommunityRatingCount);
            Assert.Equal("https://example.com", model.WikipediaUrl);
            Assert.Equal("T", model.Esrb);
            Assert.Single(model.Genres);
            Assert.Equal("RPG", model.Genres[0]);
            Assert.Single(model.Companies);
            Assert.Equal("Dev Studio", model.Companies[0]);
        }
    }
}
