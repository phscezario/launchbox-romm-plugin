using RommPlugin.Core.Helpers;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Executes the shared per-record uninstall sequence used by every entry
    /// point: clear the LaunchBox references, delete the files, mark the record
    /// uninstalled, drop the queue entry and sweep leftovers. Returns the delete
    /// outcome so callers keep their own reporting cadence (batch dialog vs
    /// single log line).
    /// </summary>
    public interface IGameUninstaller
    {
        /// <summary>
        /// Uninstalls one record. A <see cref="GamePathDeleteOutcome.Failed"/>
        /// result leaves the record installed and the queue entry untouched.
        /// </summary>
        /// <param name="record">The installed record to remove.</param>
        /// <param name="romsPath">The configured RomM roms root.</param>
        GamePathDeleteOutcome Uninstall(InstalledGameRecord record, string romsPath);
    }
}
