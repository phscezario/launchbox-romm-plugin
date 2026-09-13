using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Unified row for the Game Manager grid: exactly one row per game,
    /// merging the download queue state with the installed games record.
    /// </summary>
    public sealed class GameManagerRow
    {
        /// <summary>
        /// Gets or sets the RomM game identifier (merge key).
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the queue item, when the game has any entry in the download queue.
        /// </summary>
        public DownloadItem QueueItem { get; set; }

        /// <summary>
        /// Gets or sets the installed record, when the game has been registered as installed.
        /// </summary>
        public InstalledGameRecord Installed { get; set; }

        /// <summary>
        /// Whether this row shows live queue details (progress/status from the queue).
        /// Terminal queue states (<see cref="DownloadStatus.Installed"/> and
        /// <see cref="DownloadStatus.Completed"/>) collapse into the installed display
        /// so the game keeps a single line owned by the installed record.
        /// </summary>
        public bool ShowsQueueDetails
        {
            get
            {
                if (QueueItem == null) return false;
                return QueueItem.Status != DownloadStatus.Installed
                    && QueueItem.Status != DownloadStatus.Completed;
            }
        }

        /// <summary>
        /// Whether the game is currently installed (record exists and not uninstalled).
        /// </summary>
        public bool IsInstalledActive
        {
            get { return Installed != null && !Installed.UninstalledAt.HasValue; }
        }

        /// <summary>
        /// Display name, preferring the queue name while a queue entry exists.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (QueueItem != null && !string.IsNullOrEmpty(QueueItem.GameName))
                    return QueueItem.GameName;
                return Installed?.Title ?? string.Empty;
            }
        }

        /// <summary>
        /// Merges queue items and installed records into exactly one row per game id.
        /// Queue order is preserved first, then installed-only games in their own order.
        /// </summary>
        public static List<GameManagerRow> Merge(
            IEnumerable<DownloadItem> queueItems,
            IEnumerable<InstalledGameRecord> installedRecords)
        {
            var map = new Dictionary<int, GameManagerRow>();
            var order = new List<int>();

            if (queueItems != null)
            {
                foreach (var item in queueItems)
                {
                    if (item == null) continue;
                    if (map.TryGetValue(item.GameId, out var existing))
                    {
                        if (existing.QueueItem == null)
                            existing.QueueItem = item;
                        continue;
                    }

                    map[item.GameId] = new GameManagerRow
                    {
                        GameId = item.GameId,
                        QueueItem = item
                    };
                    order.Add(item.GameId);
                }
            }

            if (installedRecords != null)
            {
                foreach (var record in installedRecords)
                {
                    if (record == null) continue;
                    if (map.TryGetValue(record.RommGameId, out var existing))
                    {
                        existing.Installed = record;
                        continue;
                    }

                    map[record.RommGameId] = new GameManagerRow
                    {
                        GameId = record.RommGameId,
                        Installed = record
                    };
                    order.Add(record.RommGameId);
                }
            }

            var result = new List<GameManagerRow>(order.Count);
            foreach (var id in order)
                result.Add(map[id]);
            return result;
        }
    }
}
