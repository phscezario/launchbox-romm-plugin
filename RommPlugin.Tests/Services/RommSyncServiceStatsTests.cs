using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using RommPlugin.ApiClient;
using RommPlugin.Core.Models;
using RommPlugin.Services;
using RommPlugin.Tests.Helpers;
using Unbroken.LaunchBox.Plugins.Data;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class RommSyncServiceStatsTests
    {
        private const string BaseUrl = "http://localhost:9000";

        private RommApiClient CreateApiClient(MockHttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            return new RommApiClient(client);
        }

        [Fact]
        public async Task FetchLatestStatsFromRomm_ReturnsCorrectStats()
        {
            var sessions = new List<PlaySessionSchema>
            {
                new PlaySessionSchema { Id = 1, RomId = 100, DurationMs = 1800000, StartTime = DateTime.UtcNow.AddHours(-2), EndTime = DateTime.UtcNow.AddHours(-1) },
                new PlaySessionSchema { Id = 2, RomId = 100, DurationMs = 3600000, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow },
                new PlaySessionSchema { Id = 3, RomId = 100, DurationMs = 900000, StartTime = DateTime.UtcNow.AddMinutes(-30), EndTime = DateTime.UtcNow }
            };
            var json = JsonConvert.SerializeObject(sessions);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var api = CreateApiClient(handler);
            var service = new RommSyncService();
            service.SetApi(api);

            var stats = await service.FetchLatestStatsFromRomm(100);

            Assert.Equal(3, stats.PlayCount);
            Assert.Equal(6300000, stats.TotalPlayTimeMs);
            Assert.Equal(6300, stats.TotalPlayTimeSeconds);
            Assert.NotNull(stats.LastPlayed);
        }

        [Fact]
        public async Task FetchLatestStatsFromRomm_ReturnsEmptyOnNoSessions()
        {
            var json = "[]";
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var api = CreateApiClient(handler);
            var service = new RommSyncService();
            service.SetApi(api);

            var stats = await service.FetchLatestStatsFromRomm(100);

            Assert.Equal(0, stats.PlayCount);
            Assert.Equal(0, stats.TotalPlayTimeMs);
            Assert.Equal(0, stats.TotalPlayTimeSeconds);
            Assert.Null(stats.LastPlayed);
        }

        [Fact]
        public async Task FetchLatestStatsFromRomm_ReturnsEmptyOnError()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var api = CreateApiClient(handler);
            var service = new RommSyncService();
            service.SetApi(api);

            var stats = await service.FetchLatestStatsFromRomm(100);

            Assert.Equal(0, stats.PlayCount);
            Assert.Equal(0, stats.TotalPlayTimeMs);
        }

        [Fact]
        public void CompareAndUpdateStats_UpdatesWhenRommMoreRecent()
        {
            var game = new Mock<IGame>();
            game.SetupProperty(g => g.PlayCount, 5);
            game.SetupProperty(g => g.PlayTime, 1000);
            game.SetupProperty(g => g.LastPlayedDate, DateTime.UtcNow.AddDays(-2));
            game.SetupProperty(g => g.Title, "Test Game");

            var rommStats = new RommStats
            {
                PlayCount = 10,
                TotalPlayTimeMs = 7200000,
                LastPlayed = DateTime.UtcNow
            };

            var service = new RommSyncService();
            service.CompareAndUpdateStats(game.Object, rommStats);

            Assert.Equal(10, game.Object.PlayCount);
            Assert.Equal(7200, game.Object.PlayTime);
            Assert.Equal(rommStats.LastPlayed.Value, game.Object.LastPlayedDate.Value);
        }

        [Fact]
        public void CompareAndUpdateStats_KeepsLaunchBoxWhenMoreRecent()
        {
            var lbLastPlayed = DateTime.UtcNow;
            var game = new Mock<IGame>();
            game.SetupProperty(g => g.PlayCount, 5);
            game.SetupProperty(g => g.PlayTime, 1000);
            game.SetupProperty(g => g.LastPlayedDate, lbLastPlayed);
            game.SetupProperty(g => g.Title, "Test Game");

            var rommStats = new RommStats
            {
                PlayCount = 10,
                TotalPlayTimeMs = 7200000,
                LastPlayed = DateTime.UtcNow.AddDays(-5)
            };

            var service = new RommSyncService();
            service.CompareAndUpdateStats(game.Object, rommStats);

            Assert.Equal(5, game.Object.PlayCount);
            Assert.Equal(1000, game.Object.PlayTime);
            Assert.Equal(lbLastPlayed, game.Object.LastPlayedDate.Value);
        }

        [Fact]
        public void CompareAndUpdateStats_UpdatesWhenLaunchboxNull()
        {
            var game = new Mock<IGame>();
            game.SetupProperty(g => g.PlayCount, 0);
            game.SetupProperty(g => g.PlayTime, 0);
            game.SetupProperty(g => g.LastPlayedDate, (DateTime?)null);
            game.SetupProperty(g => g.Title, "Test Game");

            var rommStats = new RommStats
            {
                PlayCount = 3,
                TotalPlayTimeMs = 5400000,
                LastPlayed = DateTime.UtcNow.AddHours(-1)
            };

            var service = new RommSyncService();
            service.CompareAndUpdateStats(game.Object, rommStats);

            Assert.Equal(3, game.Object.PlayCount);
            Assert.Equal(5400, game.Object.PlayTime);
            Assert.Equal(rommStats.LastPlayed.Value, game.Object.LastPlayedDate.Value);
        }

        [Fact]
        public void CompareAndUpdateStats_DoesNothingWhenRommNull()
        {
            var game = new Mock<IGame>();
            game.SetupProperty(g => g.PlayCount, 5);
            game.SetupProperty(g => g.PlayTime, 1000);
            game.SetupProperty(g => g.LastPlayedDate, DateTime.UtcNow);
            game.SetupProperty(g => g.Title, "Test Game");

            var rommStats = new RommStats
            {
                PlayCount = 10,
                TotalPlayTimeMs = 7200000,
                LastPlayed = null
            };

            var service = new RommSyncService();
            service.CompareAndUpdateStats(game.Object, rommStats);

            Assert.Equal(5, game.Object.PlayCount);
            Assert.Equal(1000, game.Object.PlayTime);
        }

        [Fact]
        public async Task SendPlaySessionToRomm_SendsCorrectPayload()
        {
            HttpRequestMessage capturedRequest = null;
            string capturedBody = null;
            var handler = new MockHttpMessageHandler(async (req, ct) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                    capturedBody = await req.Content.ReadAsStringAsync();
                var response = new PlaySessionIngestResponse { CreatedCount = 1 };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
                };
            });
            var api = CreateApiClient(handler);
            var service = new RommSyncService();
            service.SetApi(api);

            var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            var endTime = new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc);

            await service.SendPlaySessionToRomm(42, startTime, endTime, 3600);

            Assert.NotNull(capturedRequest);
            Assert.Contains("\"rom_id\":42", capturedBody);
            Assert.Contains("\"duration_ms\":3600", capturedBody);
            Assert.Contains("\"device_id\":\"launchbox\"", capturedBody);
        }

        [Fact]
        public async Task SendPlaySessionToRomm_CallsUpdateLastPlayed()
        {
            var handler = new MockHttpMessageHandler(async (req, ct) =>
            {
                if (req.RequestUri.ToString().Contains("play-sessions"))
                {
                    var response = new PlaySessionIngestResponse { CreatedCount = 1 };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var api = CreateApiClient(handler);
            var service = new RommSyncService();
            service.SetApi(api);

            await service.SendPlaySessionToRomm(42, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 3600);

            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains("/api/play-sessions", handler.Requests[0].RequestUri.ToString());
            Assert.Contains("api/roms/42/props", handler.Requests[1].RequestUri.ToString());
        }
    }
}
