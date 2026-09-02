using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents the synchronization event log file.
    /// </summary>
    public class RommSyncFile
    {
        /// <summary>
        /// Gets or sets the file format version number.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets the list of synchronization events.
        /// </summary>
        public List<RommSyncEvent> Events { get; set; } = new List<RommSyncEvent>();
    }
}
