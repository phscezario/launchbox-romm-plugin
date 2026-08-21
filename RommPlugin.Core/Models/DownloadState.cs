using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class DownloadState
    {
        public List<DownloadItem> Items { get; set; } = new List<DownloadItem>();
        public DateTime LastUpdated { get; set; }
    }

    public class QueueAction
    {
        public string Action { get; set; }
        public int GameId { get; set; }
        public string GameName { get; set; }
        public string FsName { get; set; }
        public string FsPath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
