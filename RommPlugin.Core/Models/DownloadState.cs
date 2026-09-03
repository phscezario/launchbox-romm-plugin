using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents the current state of all downloads in the download manager.
    /// </summary>
    public class DownloadState
    {
        /// <summary>
        /// Gets or sets the list of download items being managed.
        /// </summary>
        public List<DownloadItem> Items { get; set; } = new List<DownloadItem>();

        /// <summary>
        /// Gets or sets the timestamp of the last state update.
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents an action to be performed on a download queue.
    /// </summary>
    public class QueueAction
    {
        /// <summary>
        /// Gets or sets the action to perform (e.g., "pause", "resume", "cancel").
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the RomM game identifier.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the game.
        /// </summary>
        public string GameName { get; set; }

        /// <summary>
        /// Gets or sets the filesystem name of the ROM.
        /// </summary>
        public string FsName { get; set; }

        /// <summary>
        /// Gets or sets the filesystem path on the server.
        /// </summary>
        public string FsPath { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was queued.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
