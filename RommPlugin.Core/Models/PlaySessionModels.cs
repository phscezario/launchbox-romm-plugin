using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class PlaySessionIngestPayload
    {
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        [JsonProperty("sessions")]
        public List<PlaySessionEntry> Sessions { get; set; } = new List<PlaySessionEntry>();
    }

    public class PlaySessionEntry
    {
        [JsonProperty("rom_id")]
        public int? RomId { get; set; }

        [JsonProperty("save_slot")]
        public string SaveSlot { get; set; }

        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        [JsonProperty("duration_ms")]
        public long DurationMs { get; set; }
    }

    public class PlaySessionIngestResponse
    {
        [JsonProperty("results")]
        public List<PlaySessionIngestResult> Results { get; set; } = new List<PlaySessionIngestResult>();

        [JsonProperty("created_count")]
        public int CreatedCount { get; set; }

        [JsonProperty("skipped_count")]
        public int SkippedCount { get; set; }
    }

    public class PlaySessionIngestResult
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }
    }

    public class PlaySessionSchema
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("user_id")]
        public int UserId { get; set; }

        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        [JsonProperty("rom_id")]
        public int? RomId { get; set; }

        [JsonProperty("sync_session_id")]
        public int? SyncSessionId { get; set; }

        [JsonProperty("save_slot")]
        public string SaveSlot { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime EndTime { get; set; }

        [JsonProperty("duration_ms")]
        public long DurationMs { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class RommStats
    {
        public int PlayCount { get; set; }

        public long TotalPlayTimeMs { get; set; }

        public int TotalPlayTimeSeconds => (int)(TotalPlayTimeMs / 1000);

        public DateTime? LastPlayed { get; set; }
    }
}
