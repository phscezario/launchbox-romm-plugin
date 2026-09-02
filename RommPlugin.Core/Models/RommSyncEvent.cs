using System;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a single synchronization event for a game.
    /// </summary>
    public class RommSyncEvent
    {
        /// <summary>
        /// Gets or sets the RomM game identifier.
        /// </summary>
        public int RommGameId { get; set; }

        /// <summary>
        /// Gets or sets the action performed (e.g., "added", "updated", "removed").
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
