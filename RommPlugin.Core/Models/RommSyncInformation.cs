using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class RommSyncInformation
    {
        public bool SyncInProgress { get; set; }

        public List<int> CompletedPlatformIds { get; set; } = new List<int>();

        public Dictionary<int, List<int>> CompletedGameIdsByPlatform { get; set; } = new Dictionary<int, List<int>>();

        public List<int> UnselectedPlatformIds { get; set; } = new List<int>();
    }
}
