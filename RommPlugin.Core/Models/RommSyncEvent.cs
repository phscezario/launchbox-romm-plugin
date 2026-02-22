using System;

namespace RommPlugin.Core.Models
{
    public class RommSyncEvent
    {
        public int RommGameId { get; set; }

        public string Action { get; set; }

        public DateTime Timestamp { get; set; }
    }
}

