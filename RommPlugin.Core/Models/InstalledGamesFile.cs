using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents the file that tracks installed games.
    /// </summary>
    public class InstalledGamesFile
    {
        /// <summary>
        /// Gets or sets the file format version number.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets the list of installed game records.
        /// </summary>
        public List<InstalledGameRecord> Games { get; set; } = new List<InstalledGameRecord>();
    }

    /// <summary>
    /// Represents a record of an installed game.
    /// </summary>
    public class InstalledGameRecord
    {
        /// <summary>
        /// Gets or sets the RomM game identifier.
        /// </summary>
        public int RommGameId { get; set; }

        /// <summary>
        /// Gets or sets the display title of the game.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the platform name.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Gets or sets the game category (e.g., "rom", "folder").
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the remote file path on the RomM server.
        /// </summary>
        public string RemotePath { get; set; }

        /// <summary>
        /// Gets or sets the filename of the ROM.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the local path where the game is installed.
        /// </summary>
        public string InstalledPath { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the game was installed.
        /// </summary>
        public DateTime InstalledAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the game was uninstalled, or null if still installed.
        /// </summary>
        public DateTime? UninstalledAt { get; set; }
    }
}
