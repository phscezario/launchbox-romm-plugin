using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    public class PlaySessionModelsTests
    {
        [Fact]
        public void PlaySessionIngestPayload_SerializesCorrectly()
        {
            var payload = new PlaySessionIngestPayload
            {
                DeviceId = "launchbox",
                Sessions = new List<PlaySessionEntry>
                {
                    new PlaySessionEntry
                    {
                        RomId = 123,
                        StartTime = "2026-01-15T10:00:00Z",
                        EndTime = "2026-01-15T11:00:00Z",
                        DurationMs = 3600000
                    }
                }
            };

            var json = JsonConvert.SerializeObject(payload);

            Assert.Contains("\"device_id\":\"launchbox\"", json);
            Assert.Contains("\"rom_id\":123", json);
            Assert.Contains("\"duration_ms\":3600000", json);
        }

        [Fact]
        public void PlaySessionIngestPayload_DeserializesCorrectly()
        {
            var json = "{\"device_id\":\"launchbox\",\"sessions\":[{\"rom_id\":123,\"start_time\":\"2026-01-15T10:00:00Z\",\"end_time\":\"2026-01-15T11:00:00Z\",\"duration_ms\":3600000}]}";
            var payload = JsonConvert.DeserializeObject<PlaySessionIngestPayload>(json);

            Assert.NotNull(payload);
            Assert.Equal("launchbox", payload.DeviceId);
            Assert.Single(payload.Sessions);
            Assert.Equal(123, payload.Sessions[0].RomId);
            Assert.Equal(3600000, payload.Sessions[0].DurationMs);
        }

        [Fact]
        public void PlaySessionIngestPayload_DefaultSessionsIsEmpty()
        {
            var payload = new PlaySessionIngestPayload();
            Assert.NotNull(payload.Sessions);
            Assert.Empty(payload.Sessions);
        }

        [Fact]
        public void PlaySessionEntry_SerializesCorrectly()
        {
            var entry = new PlaySessionEntry
            {
                RomId = 456,
                SaveSlot = "slot1",
                StartTime = "2026-01-15T10:00:00Z",
                EndTime = "2026-01-15T10:30:00Z",
                DurationMs = 1800000
            };

            var json = JsonConvert.SerializeObject(entry);

            Assert.Contains("\"rom_id\":456", json);
            Assert.Contains("\"save_slot\":\"slot1\"", json);
            Assert.Contains("\"duration_ms\":1800000", json);
        }

        [Fact]
        public void PlaySessionEntry_NullRomId_SerializesAsNull()
        {
            var entry = new PlaySessionEntry
            {
                RomId = null,
                StartTime = "2026-01-15T10:00:00Z",
                EndTime = "2026-01-15T10:30:00Z",
                DurationMs = 1800000
            };

            var json = JsonConvert.SerializeObject(entry);
            var deserialized = JsonConvert.DeserializeObject<PlaySessionEntry>(json);

            Assert.Null(deserialized.RomId);
        }

        [Fact]
        public void PlaySessionIngestResponse_DeserializesCorrectly()
        {
            var json = "{\"results\":[{\"index\":0,\"status\":\"created\",\"id\":1,\"detail\":null}],\"created_count\":1,\"skipped_count\":0}";
            var response = JsonConvert.DeserializeObject<PlaySessionIngestResponse>(json);

            Assert.NotNull(response);
            Assert.Equal(1, response.CreatedCount);
            Assert.Equal(0, response.SkippedCount);
            Assert.Single(response.Results);
            Assert.Equal("created", response.Results[0].Status);
            Assert.Equal(1, response.Results[0].Id);
        }

        [Fact]
        public void PlaySessionIngestResponse_HandlesDuplicateStatus()
        {
            var json = "{\"results\":[{\"index\":0,\"status\":\"duplicate\",\"id\":null,\"detail\":\"Already exists\"}],\"created_count\":0,\"skipped_count\":1}";
            var response = JsonConvert.DeserializeObject<PlaySessionIngestResponse>(json);

            Assert.Equal(0, response.CreatedCount);
            Assert.Equal(1, response.SkippedCount);
            Assert.Equal("duplicate", response.Results[0].Status);
            Assert.Null(response.Results[0].Id);
            Assert.Equal("Already exists", response.Results[0].Detail);
        }

        [Fact]
        public void PlaySessionSchema_DeserializesCorrectly()
        {
            var json = "{\"id\":1,\"user_id\":10,\"device_id\":\"launchbox\",\"rom_id\":123,\"start_time\":\"2026-01-15T10:00:00Z\",\"end_time\":\"2026-01-15T11:00:00Z\",\"duration_ms\":3600000,\"created_at\":\"2026-01-15T10:00:00Z\",\"updated_at\":\"2026-01-15T11:00:00Z\"}";
            var session = JsonConvert.DeserializeObject<PlaySessionSchema>(json);

            Assert.NotNull(session);
            Assert.Equal(1, session.Id);
            Assert.Equal(10, session.UserId);
            Assert.Equal("launchbox", session.DeviceId);
            Assert.Equal(123, session.RomId);
            Assert.Equal(3600000, session.DurationMs);
            Assert.Equal(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), session.StartTime);
            Assert.Equal(new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc), session.EndTime);
        }

        [Fact]
        public void PlaySessionSchema_NullRomId_DeserializesCorrectly()
        {
            var json = "{\"id\":1,\"user_id\":10,\"rom_id\":null,\"start_time\":\"2026-01-15T10:00:00Z\",\"end_time\":\"2026-01-15T11:00:00Z\",\"duration_ms\":3600000}";
            var session = JsonConvert.DeserializeObject<PlaySessionSchema>(json);

            Assert.NotNull(session);
            Assert.Null(session.RomId);
        }

        [Fact]
        public void PlaySessionIngestResult_DeserializesCorrectly()
        {
            var json = "{\"index\":0,\"status\":\"created\",\"id\":42,\"detail\":null}";
            var result = JsonConvert.DeserializeObject<PlaySessionIngestResult>(json);

            Assert.NotNull(result);
            Assert.Equal(0, result.Index);
            Assert.Equal("created", result.Status);
            Assert.Equal(42, result.Id);
            Assert.Null(result.Detail);
        }

        [Fact]
        public void PlaySessionIngestResult_ErrorStatus_HasDetail()
        {
            var json = "{\"index\":1,\"status\":\"error\",\"id\":null,\"detail\":\"Invalid rom_id\"}";
            var result = JsonConvert.DeserializeObject<PlaySessionIngestResult>(json);

            Assert.Equal("error", result.Status);
            Assert.Null(result.Id);
            Assert.Equal("Invalid rom_id", result.Detail);
        }
    }
}
