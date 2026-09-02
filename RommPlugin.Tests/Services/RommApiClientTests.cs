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

        [Fact]
        public async Task GetAllGamesByPlatformAsync_SinglePage()
        {
            var response = new RommGameResponse
            {
                Total = 1,
                Items = new List<RommGame>
                {
                    new RommGame { Id = 1, Name = "Game 1" }
                }
            };
            var json = JsonConvert.SerializeObject(response);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.GetAllGamesByPlatformAsync(10);

            Assert.Single(result);
            Assert.Equal("Game 1", result[0].Name);
        }

        [Fact]
        public async Task GetAllGamesByPlatformAsync_MultiplePages()
        {
            var page1 = new RommGameResponse
            {
                Total = 2,
                Items = new List<RommGame>
                {
                    new RommGame { Id = 1, Name = "Game 1" }
                }
            };
            var page2 = new RommGameResponse
            {
                Total = 2,
                Items = new List<RommGame>
                {
                    new RommGame { Id = 2, Name = "Game 2" }
                }
            };

            int callCount = 0;
            var handler = new MockHttpMessageHandler(async (req, ct) =>
            {
                callCount++;
                var data = callCount == 1 ? page1 : page2;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")
                };
            });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.GetAllGamesByPlatformAsync(10);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task GetAllGamesByPlatformAsync_EmptyResult()
        {
            var response = new RommGameResponse { Total = 0, Items = new List<RommGame>() };
            var json = JsonConvert.SerializeObject(response);
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.GetAllGamesByPlatformAsync(10);

            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateGameById_Succeeds()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var request = new RommUpdateGameRequest { Name = "Updated", Summary = "Summary" };
            await api.UpdateGameById(42, request);

            Assert.Single(handler.Requests);
            Assert.Contains("/api/roms/42", handler.Requests[0].RequestUri.ToString());
            Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        }

        [Fact]
        public async Task UpdateGameById_RetriesOn500()
        {
            int callCount = 0;
            var handler = new MockHttpMessageHandler(async (req, ct) =>
            {
                callCount++;
                if (callCount < 3)
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var request = new RommUpdateGameRequest { Name = "Updated" };
            await api.UpdateGameById(42, request);

            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task UpdateGameById_DoesNotRetryOn400()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Invalid data")
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var request = new RommUpdateGameRequest { Name = "Updated" };

            await Assert.ThrowsAsync<ClientErrorException>(
                () => api.UpdateGameById(42, request));
        }

        [Fact]
        public async Task RemoveGameMetadataById_Succeeds()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            await api.RemoveGameMetadataById(42);

            Assert.Single(handler.Requests);
            Assert.Contains("/api/roms/42", handler.Requests[0].RequestUri.ToString());
            Assert.Contains("unmatch_metadata=true", handler.Requests[0].RequestUri.ToString());
            Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        }

        [Fact]
        public async Task RemoveGameMetadataById_RetriesOn500()
        {
            int callCount = 0;
            var handler = new MockHttpMessageHandler(async (req, ct) =>
            {
                callCount++;
                if (callCount < 2)
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            await api.RemoveGameMetadataById(42);

            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task DownloadBytesAsync_RelativeUrl()
        {
            var expectedBytes = new byte[] { 1, 2, 3 };
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(expectedBytes)
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.DownloadBytesAsync("/api/screenshots/1/file");

            Assert.Equal(expectedBytes, result);
        }

        [Fact]
        public async Task DownloadBytesAsync_AbsoluteUrl()
        {
            var expectedBytes = new byte[] { 4, 5, 6 };
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(expectedBytes)
                });
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            var result = await api.DownloadBytesAsync($"{BaseUrl}/api/screenshots/1/file");

            Assert.Equal(expectedBytes, result);
        }

        [Fact]
        public void SetBasicAuthentication_SetsCorrectHeader()
        {
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            api.SetBasicAuthentication("user", "pass");

            var header = client.DefaultRequestHeaders.Authorization;
            Assert.Equal("Basic", header.Scheme);
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            Assert.Equal("user:pass", decoded);
        }

        [Fact]
        public void SetBearerAuthentication_SetsCorrectHeader()
        {
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            api.SetBearerAuthentication("my-token-123");

            var header = client.DefaultRequestHeaders.Authorization;
            Assert.Equal("Bearer", header.Scheme);
            Assert.Equal("my-token-123", header.Parameter);
        }

        [Fact]
        public void ApplyAuthentication_UsesBearer_WhenTokenProvided()
        {
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            api.ApplyAuthentication(new RommPluginSettings
            {
                ClientApiToken = "api-token",
                Username = "user",
                Password = "pass"
            });

            var header = client.DefaultRequestHeaders.Authorization;
            Assert.Equal("Bearer", header.Scheme);
            Assert.Equal("api-token", header.Parameter);
        }

        [Fact]
        public void ApplyAuthentication_UsesBasic_WhenTokenEmpty()
        {
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            var api = new RommApiClient(client);

            api.ApplyAuthentication(new RommPluginSettings
            {
                ClientApiToken = "",
                Username = "admin",
                Password = "secret"
            });

            var header = client.DefaultRequestHeaders.Authorization;
            Assert.Equal("Basic", header.Scheme);
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            Assert.Equal("admin:secret", decoded);
        }
    }
}
