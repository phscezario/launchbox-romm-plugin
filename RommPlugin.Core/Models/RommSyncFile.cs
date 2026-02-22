using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class RommSyncFile
    {
        public int Version { get; set; } = 1;

        public List<RommSyncEvent> Events { get; set; } = new List<RommSyncEvent>();
    }
}

