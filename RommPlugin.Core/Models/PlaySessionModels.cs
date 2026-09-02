using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents the payload for ingesting play session data to the RomM server.
    /// </summary>
    public class PlaySessionIngestPayload
    {
        /// <summary>
        /// Gets or sets the unique device identifier.
        /// </summary>
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the list of play session entries to ingest.
        /// </summary>
        [JsonProperty("sessions")]
        public List<PlaySessionEntry> Sessions { get; set; } = new List<PlaySessionEntry>();
    }

    /// <summary>
    /// Represents a single play session entry for ingestion.
    /// </summary>
    public class PlaySessionEntry
    {
        /// <summary>
        /// Gets or sets the ROM identifier.
        /// </summary>
        [JsonProperty("rom_id")]
        public int? RomId { get; set; }

        /// <summary>
        /// Gets or sets the save slot identifier.
        /// </summary>
        [JsonProperty("save_slot")]
        public string SaveSlot { get; set; }

        /// <summary>
        /// Gets or sets the session start time as an ISO 8601 string.
        /// </summary>
        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the session end time as an ISO 8601 string.
        /// </summary>
        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets the session duration in milliseconds.
        /// </summary>
        [JsonProperty("duration_ms")]
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// Represents the response from a play session ingestion request.
    /// </summary>
    public class PlaySessionIngestResponse
    {
        /// <summary>
        /// Gets or sets the list of ingestion results for each session.
        /// </summary>
        [JsonProperty("results")]
        public List<PlaySessionIngestResult> Results { get; set; } = new List<PlaySessionIngestResult>();

        /// <summary>
        /// Gets or sets the number of sessions that were newly created.
        /// </summary>
        [JsonProperty("created_count")]
        public int CreatedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of sessions that were skipped (duplicates).
        /// </summary>
        [JsonProperty("skipped_count")]
        public int SkippedCount { get; set; }
    }

    /// <summary>
    /// Represents the result of ingesting a single play session.
    /// </summary>
    public class PlaySessionIngestResult
    {
        /// <summary>
        /// Gets or sets the index of the session in the original request.
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the status of the ingestion (e.g., "created", "skipped").
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the server-assigned session identifier, if created.
        /// </summary>
        [JsonProperty("id")]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets additional detail about the result.
        /// </summary>
        [JsonProperty("detail")]
        public string Detail { get; set; }
    }

    /// <summary>
    /// Represents a play session record stored on the server.
    /// </summary>
    public class PlaySessionSchema
    {
        /// <summary>
        /// Gets or sets the unique session identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        [JsonProperty("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the ROM identifier.
        /// </summary>
        [JsonProperty("rom_id")]
        public int? RomId { get; set; }

        /// <summary>
        /// Gets or sets the sync session identifier.
        /// </summary>
        [JsonProperty("sync_session_id")]
        public int? SyncSessionId { get; set; }

        /// <summary>
        /// Gets or sets the save slot identifier.
        /// </summary>
        [JsonProperty("save_slot")]
        public string SaveSlot { get; set; }

        /// <summary>
        /// Gets or sets the session start time.
        /// </summary>
        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the session end time.
        /// </summary>
        [JsonProperty("end_time")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the session duration in milliseconds.
        /// </summary>
        [JsonProperty("duration_ms")]
        public long DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents aggregated play statistics for a game.
    /// </summary>
    public class RommStats
    {
        /// <summary>
        /// Gets or sets the total number of times the game has been played.
        /// </summary>
        public int PlayCount { get; set; }

        /// <summary>
        /// Gets or sets the total play time in milliseconds.
        /// </summary>
        public long TotalPlayTimeMs { get; set; }

        /// <summary>
        /// Gets the total play time in seconds.
        /// </summary>
        public int TotalPlayTimeSeconds => (int)(TotalPlayTimeMs / 1000);

        /// <summary>
        /// Gets or sets the timestamp of the last play session.
        /// </summary>
        public DateTime? LastPlayed { get; set; }
    }
}
