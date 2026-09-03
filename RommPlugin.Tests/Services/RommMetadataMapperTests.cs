using System;
using Moq;
using RommPlugin.Core.Models;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins.Data;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class RommMetadataMapperTests
    {
        private readonly RommMetadataMapper _mapper = new RommMetadataMapper();

        private static Mock<IGame> CreateGame(DateTime? releaseDate = null, int? maxPlayers = null,
            string playMode = null, string videoUrl = null, float communityRating = 0,
            int communityVotes = 0, string notes = null, int? launchBoxDbId = null)
        {
            var game = new Mock<IGame>();
            game.SetupProperty(g => g.ReleaseDate, releaseDate);
            game.SetupProperty(g => g.MaxPlayers, maxPlayers);
            game.SetupProperty(g => g.PlayMode, playMode);
            game.SetupProperty(g => g.VideoUrl, videoUrl);
            game.SetupProperty(g => g.CommunityStarRating, communityRating);
            game.SetupProperty(g => g.CommunityStarRatingTotalVotes, communityVotes);
            game.SetupProperty(g => g.Notes, notes);
            game.SetupProperty(g => g.LaunchBoxDbId, launchBoxDbId);
            return game;
        }

        private static RommPluginSettings CreateSettings(bool keepLocalData = false)
        {
            return new RommPluginSettings { KeepLocalData = keepLocalData };
        }

        [Fact]
        public void ApplyReleaseDate_PrefersLaunchBoxMetadata()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { FirstReleaseDate = 1640995200 }; // 2022-01-01 UTC (seconds)
            var ss = new SsMetadata { ReleaseDate = "2023-06-15" };
            var igdb = new IgdbMetadata { FirstReleaseDate = 1700000000 };
            var meta = new RommGameMeta { FirstReleaseDate = 1800000000 };

            _mapper.ApplyReleaseDate(game.Object, lb, ss, igdb, meta, overwrite: true);

            var expected = DateTimeOffset.FromUnixTimeSeconds(1640995200).DateTime;
            Assert.Equal(expected, game.Object.ReleaseDate.Value);
        }

        [Fact]
        public void ApplyReleaseDate_FallsToScreenscraper_WhenLaunchBoxNull()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { FirstReleaseDate = null };
            var ss = new SsMetadata { ReleaseDate = "2023-06-15" };
            var igdb = new IgdbMetadata { FirstReleaseDate = null };
            var meta = new RommGameMeta { FirstReleaseDate = null };

            _mapper.ApplyReleaseDate(game.Object, lb, ss, igdb, meta, overwrite: true);

            Assert.Equal(new DateTime(2023, 6, 15), game.Object.ReleaseDate.Value);
        }

        [Fact]
        public void ApplyReleaseDate_PreservesExistingDate_WhenOverwriteFalse()
        {
            var existingDate = new DateTime(2020, 1, 1);
            var game = CreateGame(releaseDate: existingDate);
            var lb = new LaunchBoxMetadataModel { FirstReleaseDate = 1640995200 };

            _mapper.ApplyReleaseDate(game.Object, lb, null, null, null, overwrite: false);

            Assert.Equal(existingDate, game.Object.ReleaseDate.Value);
        }

        [Fact]
        public void ApplyReleaseDate_SetsDate_WhenOverwriteFalseAndExistingNull()
        {
            var game = CreateGame(releaseDate: null);
            var lb = new LaunchBoxMetadataModel { FirstReleaseDate = 1640995200 };

            _mapper.ApplyReleaseDate(game.Object, lb, null, null, null, overwrite: false);

            var expected = DateTimeOffset.FromUnixTimeSeconds(1640995200).DateTime;
            Assert.Equal(expected, game.Object.ReleaseDate.Value);
        }

        [Fact]
        public void ApplyMaxPlayers_PrefersLaunchBoxMetadata()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { MaxPlayers = 4 };
            var ss = new SsMetadata { Players = "2" };

            _mapper.ApplyMaxPlayers(game.Object, lb, ss, overwrite: true);

            Assert.Equal(4, game.Object.MaxPlayers);
        }

        [Fact]
        public void ApplyMaxPlayers_ParsesSsPlayersString()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { MaxPlayers = null };
            var ss = new SsMetadata { Players = "8" };

            _mapper.ApplyMaxPlayers(game.Object, lb, ss, overwrite: true);

            Assert.Equal(8, game.Object.MaxPlayers);
        }

        [Fact]
        public void ApplyMaxPlayers_PreservesExisting_WhenOverwriteFalse()
        {
            var game = CreateGame(maxPlayers: 2);
            var lb = new LaunchBoxMetadataModel { MaxPlayers = 4 };

            _mapper.ApplyMaxPlayers(game.Object, lb, null, overwrite: false);

            Assert.Equal(2, game.Object.MaxPlayers);
        }

        [Fact]
        public void ApplyPlayMode_SetsCooperative_WhenTrue()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { Cooperative = true };

            _mapper.ApplyPlayMode(game.Object, lb, overwrite: true);

            Assert.Equal("Cooperative", game.Object.PlayMode);
        }

        [Fact]
        public void ApplyPlayMode_DoesNotSet_WhenCooperativeFalse()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { Cooperative = false };

            _mapper.ApplyPlayMode(game.Object, lb, overwrite: true);

            Assert.Null(game.Object.PlayMode);
        }

        [Fact]
        public void ApplyVideoUrl_UsesLaunchBoxVideoId()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { YoutubeVideoId = "abc123" };
            var igdb = new IgdbMetadata { YoutubeVideoId = "xyz789" };

            _mapper.ApplyVideoUrl(game.Object, lb, igdb, overwrite: true);

            Assert.Equal("https://www.youtube.com/watch?v=abc123", game.Object.VideoUrl);
        }

        [Fact]
        public void ApplyVideoUrl_FallsToIgdb_WhenLaunchBoxNull()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { YoutubeVideoId = null };
            var igdb = new IgdbMetadata { YoutubeVideoId = "xyz789" };

            _mapper.ApplyVideoUrl(game.Object, lb, igdb, overwrite: true);

            Assert.Equal("https://www.youtube.com/watch?v=xyz789", game.Object.VideoUrl);
        }

        [Fact]
        public void ApplyVideoUrl_PreservesExisting_WhenOverwriteFalse()
        {
            var game = CreateGame(videoUrl: "https://existing.url");
            var lb = new LaunchBoxMetadataModel { YoutubeVideoId = "abc123" };

            _mapper.ApplyVideoUrl(game.Object, lb, null, overwrite: false);

            Assert.Equal("https://existing.url", game.Object.VideoUrl);
        }

        [Fact]
        public void ApplyCommunityRating_PrefersLaunchBoxRating()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { CommunityRating = 4.5f, CommunityRatingCount = 100 };
            var igdb = new IgdbMetadata { TotalRating = 3.0 };
            var meta = new RommGameMeta { AverageRating = 2.0 };

            _mapper.ApplyCommunityRating(game.Object, lb, igdb, meta, overwrite: true);

            Assert.Equal(4.5f, game.Object.CommunityStarRating);
            Assert.Equal(100, game.Object.CommunityStarRatingTotalVotes);
        }

        [Fact]
        public void ApplyCommunityRating_FallsToIgdb_WhenLaunchBoxZero()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { CommunityRating = 0 };
            var igdb = new IgdbMetadata { TotalRating = 7.5 };

            _mapper.ApplyCommunityRating(game.Object, lb, igdb, null, overwrite: true);

            Assert.Equal(7.5f, game.Object.CommunityStarRating);
        }

        [Fact]
        public void ApplyCommunityRating_FallsToMeta_WhenIgdbNull()
        {
            var game = CreateGame();
            var lb = new LaunchBoxMetadataModel { CommunityRating = 0 };
            var meta = new RommGameMeta { AverageRating = 6.0 };

            _mapper.ApplyCommunityRating(game.Object, lb, null, meta, overwrite: true);

            Assert.Equal(6.0f, game.Object.CommunityStarRating);
        }

        [Fact]
        public void ApplyServerMetadata_Overwrites_WhenKeepLocalDataFalse()
        {
            var game = CreateGame(
                releaseDate: new DateTime(2020, 1, 1),
                maxPlayers: 1,
                notes: "old notes"
            );

            var rommGame = new RommGame
            {
                LaunchboxId = 42,
                Summary = "new summary",
                LaunchBoxMetadata = new LaunchBoxMetadataModel
                {
                    FirstReleaseDate = 1640995200,
                    MaxPlayers = 4,
                    Cooperative = true,
                    YoutubeVideoId = "vid123",
                    CommunityRating = 4.0f,
                    CommunityRatingCount = 50
                },
                SsMetadata = new SsMetadata { Synopsis = "ss synopsis" },
                IgdbMetadata = new IgdbMetadata { TotalRating = 8.0 }
            };

            var settings = CreateSettings(keepLocalData: false);

            _mapper.ApplyServerMetadata(game.Object, rommGame, settings);

            var expectedDate = DateTimeOffset.FromUnixTimeSeconds(1640995200).DateTime;
            Assert.Equal(expectedDate, game.Object.ReleaseDate.Value);
            Assert.Equal(4, game.Object.MaxPlayers);
            Assert.Equal("Cooperative", game.Object.PlayMode);
            Assert.Equal("https://www.youtube.com/watch?v=vid123", game.Object.VideoUrl);
            Assert.Equal(4.0f, game.Object.CommunityStarRating);
            Assert.Equal(50, game.Object.CommunityStarRatingTotalVotes);
            Assert.Equal("ss synopsis", game.Object.Notes);
            Assert.Equal((int?)42, game.Object.LaunchBoxDbId);
        }

        [Fact]
        public void ApplyServerMetadata_Preserves_WhenKeepLocalDataTrue()
        {
            var game = CreateGame(
                releaseDate: new DateTime(2020, 1, 1),
                maxPlayers: 1,
                videoUrl: "https://existing.url",
                notes: "existing notes"
            );

            var rommGame = new RommGame
            {
                LaunchBoxMetadata = new LaunchBoxMetadataModel
                {
                    FirstReleaseDate = 1640995200,
                    MaxPlayers = 4,
                    YoutubeVideoId = "newvid"
                }
            };

            var settings = CreateSettings(keepLocalData: true);

            _mapper.ApplyServerMetadata(game.Object, rommGame, settings);

            Assert.Equal(new DateTime(2020, 1, 1), game.Object.ReleaseDate.Value);
            Assert.Equal(1, game.Object.MaxPlayers);
            Assert.Equal("https://existing.url", game.Object.VideoUrl);
            Assert.Equal("existing notes", game.Object.Notes);
        }

        [Fact]
        public void ApplyServerMetadata_NotesFallsToSsDescription_WhenSynopsisNull()
        {
            var game = CreateGame(notes: null);
            var rommGame = new RommGame
            {
                SsMetadata = new SsMetadata { Synopsis = null, Description = "ss description" }
            };
            var settings = CreateSettings(keepLocalData: false);

            _mapper.ApplyServerMetadata(game.Object, rommGame, settings);

            Assert.Equal("ss description", game.Object.Notes);
        }
    }
}
