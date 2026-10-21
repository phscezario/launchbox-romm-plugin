using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Pure display ordering for the Game Manager list: games that are moving or
    /// broken (in a non-terminal, active or failed queue state) bubble to the
    /// top; games the user just uninstalled follow in their own group so they
    /// stay visible and one click away from reinstalling; everything else
    /// follows. The moving/failed and remaining groups are alphabetical by the
    /// displayed name; the uninstalled group is most recently uninstalled first.
    /// The game id is the deterministic final tie-breaker.
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
        /// True when the row is a soft-deleted install: no queue entry, only an
        /// installed record flagged as uninstalled. These sit directly below the
        /// moving/failed group, so a game the user just uninstalled stays visible
        /// instead of dissolving into the alphabetical list, with the retry
        /// action still one click away.
        /// </summary>
        public static bool IsUninstalled(GameManagerRow row)
        {
            if (row == null || row.QueueItem != null) return false;
            return row.Installed != null && row.Installed.UninstalledAt.HasValue;
        }

        /// <summary>
        /// Orders rows for display: moving/failed first, then uninstalled (most
        /// recent first), then the rest; alphabetical by
        /// <see cref="GameManagerRow.SortName"/> inside the first and last groups.
        /// </summary>
        public static IList<GameManagerRow> Order(IEnumerable<GameManagerRow> rows)
        {
            if (rows == null) return new List<GameManagerRow>();

            return rows
                .Where(r => r != null)
                .OrderBy(Rank)
                .ThenByDescending(r => IsUninstalled(r) ? r.Installed.UninstalledAt : (DateTime?)null)
                .ThenBy(r => r.SortName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.GameId)
                .ToList();
        }

        private static int Rank(GameManagerRow row)
        {
            if (IsTopPriority(row)) return 0;
            if (IsUninstalled(row)) return 1;
            return 2;
        }
    }
}
