using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class InstalledGamesFile
    {
        public int Version { get; set; } = 1;

        public List<InstalledGameRecord> Games { get; set; } = new List<InstalledGameRecord>();
    }

    public class InstalledGameRecord
    {
        public int RommGameId { get; set; }

        public string Title { get; set; }

        public string Platform { get; set; }

        public string Category { get; set; }

        public string RemotePath { get; set; }

        public string FileName { get; set; }

        public string InstalledPath { get; set; }

        public DateTime InstalledAt { get; set; }

        public DateTime? UninstalledAt { get; set; }
    }
}
