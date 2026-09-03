using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Tracks the progress and state of a synchronization operation between RomM and LaunchBox.
    /// </summary>
    public class RommSyncInformation
    {
        /// <summary>
        /// Gets or sets whether a sync operation is currently in progress.
        /// </summary>
        public bool SyncInProgress { get; set; }

        /// <summary>
        /// Gets or sets the list of platform IDs that have completed synchronization.
        /// </summary>
        public List<int> CompletedPlatformIds { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets a dictionary mapping platform IDs to their completed game IDs.
        /// </summary>
        public Dictionary<int, List<int>> CompletedGameIdsByPlatform { get; set; } = new Dictionary<int, List<int>>();

        /// <summary>
        /// Gets or sets the list of platform IDs that were not selected for synchronization.
        /// </summary>
        public List<int> UnselectedPlatformIds { get; set; } = new List<int>();
    }
}
