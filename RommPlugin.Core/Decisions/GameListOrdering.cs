using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Pure display ordering for the Game Manager list: games that are moving or
    /// broken (in a non-terminal, active or failed queue state) bubble to the
    /// top; everything else follows. Each group is alphabetical by the displayed
    /// name, with the game id as a deterministic tie-breaker.
    /// </summary>
    public static class GameListOrdering
    {
        private static readonly HashSet<DownloadStatus> ActiveStatuses = new HashSet<DownloadStatus>
        {
            DownloadStatus.Pending,
            DownloadStatus.Downloading,
            DownloadStatus.Paused,
            DownloadStatus.Failed,
            DownloadStatus.WaitingInstall,
            DownloadStatus.WaitingUninstall,
        };

        /// <summary>
        /// True when the row is in motion or failed and therefore belongs on top.
        /// </summary>
        public static bool IsTopPriority(GameManagerRow row)
        {
            if (row == null || row.QueueItem == null) return false;
            return ActiveStatuses.Contains(row.QueueItem.Status);
        }

        /// <summary>
        /// Orders rows for display: top-priority group first, then the rest, each
        /// alphabetically by <see cref="GameManagerRow.SortName"/>.
        /// </summary>
        public static IList<GameManagerRow> Order(IEnumerable<GameManagerRow> rows)
        {
            if (rows == null) return new List<GameManagerRow>();

            return rows
                .Where(r => r != null)
                .OrderBy(r => IsTopPriority(r) ? 0 : 1)
                .ThenBy(r => r.SortName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.GameId)
                .ToList();
        }
    }
}
