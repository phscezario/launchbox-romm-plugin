using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.ApiClient;
using RommPlugin.Core.Models;
using RommPlugin.Tests.Helpers;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class RommApiClientTests
    {
        private const string BaseUrl = "http://localhost:9000";

        [Fact]
        public async Task GetGameByIdAsync_ReturnsGame()
        {
            var game = new RommGame
            {
                Id = 1,
                Name = "Test Game",
                UserScreenshots = new List<RommScreenshot>
                {
                    new RommScreenshot { Id = 10, FileName = "shot.png", FileNameNoExt = "shot", FileSizeBytes = 1024 }
                }
            };
            var json = JsonConvert.SerializeObject(game);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.GetGameByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Game", result.Name);
        }

        [Fact]
        public async Task GetGameByIdAsync_CallsCorrectUrl()
        {
            var game = new RommGame { Id = 1, Name = "Test" };
            var json = JsonConvert.SerializeObject(game);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            await api.GetGameByIdAsync(42);

            Assert.Single(handler.Requests);
            Assert.Contains("/api/roms/42", handler.Requests[0].RequestUri.ToString());
        }

        [Fact]
        public async Task GetPlaySessionsAsync_ReturnsSessions()
        {
            var sessions = new List<PlaySessionSchema>
            {
                new PlaySessionSchema { Id = 1, RomId = 100, DurationMs = 3600000, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow }
            };
            var json = JsonConvert.SerializeObject(sessions);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.GetPlaySessionsAsync(100);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(100, result[0].RomId);
        }

        [Fact]
        public async Task GetPlaySessionsAsync_CallsCorrectUrl()
        {
            var json = "[]";
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            await api.GetPlaySessionsAsync(55);

            Assert.Single(handler.Requests);
            var url = handler.Requests[0].RequestUri.ToString();
            Assert.Contains("/api/play-sessions?rom_id=55", url);
            Assert.Contains("limit=1000", url);
        }

        [Fact]
        public async Task GetPlaySessionsAsync_ReturnsEmptyList()
        {
            var json = "[]";
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.GetPlaySessionsAsync(1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task IngestPlaySessionsAsync_ReturnsResponse()
        {
            var response = new PlaySessionIngestResponse { CreatedCount = 1, SkippedCount = 0 };
            var json = JsonConvert.SerializeObject(response);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var payload = new PlaySessionIngestPayload
            {
                DeviceId = "launchbox",
                Sessions = new List<PlaySessionEntry>
                {
                    new PlaySessionEntry { RomId = 1, StartTime = "2026-01-15T10:00:00Z", EndTime = "2026-01-15T11:00:00Z", DurationMs = 3600000 }
                }
            };

            var result = await api.IngestPlaySessionsAsync(payload);

            Assert.NotNull(result);
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.SkippedCount);
        }

        [Fact]
        public async Task IngestPlaySessionsAsync_SendsCorrectPayload()
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
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var payload = new PlaySessionIngestPayload
            {
                DeviceId = "launchbox",
                Sessions = new List<PlaySessionEntry>
                {
                    new PlaySessionEntry { RomId = 1, StartTime = "2026-01-15T10:00:00Z", EndTime = "2026-01-15T11:00:00Z", DurationMs = 3600000 }
                }
            };

            await api.IngestPlaySessionsAsync(payload);

            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Post, capturedRequest.Method);
            Assert.Contains("/api/play-sessions", capturedRequest.RequestUri.ToString());
            Assert.Contains("\"device_id\":\"launchbox\"", capturedBody);
            Assert.Contains("\"rom_id\":1", capturedBody);
        }

        [Fact]
        public async Task UpdateGameLastPlayedAsync_CallsCorrectUrl()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            await api.UpdateGameLastPlayedAsync(42);

            Assert.Single(handler.Requests);
            var url = handler.Requests[0].RequestUri.ToString();
            Assert.Contains("api/roms/42/props", url);
            Assert.Contains("update_last_played=true", url);
            Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        }
    }
}
