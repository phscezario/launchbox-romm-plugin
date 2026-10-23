using System.Threading.Tasks;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for processing pending download items (installs and uninstalls) from the RomM server.
    /// </summary>
    public interface IRommProcessInstallUninstallService
    {
        /// <summary>
        /// Gets a value indicating whether the last run stopped because the user
        /// cancelled it. Callers use this to avoid reopening the progress window
        /// immediately after a cancel.
        /// </summary>
        bool LastRunWasCancelled { get; }

        /// <summary>
        /// Processes all pending install events by extracting downloaded ROMs and configuring LaunchBox games.
        /// </summary>
        /// <param name="showEmptyMessage">If true, displays a message when there are no pending items.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        Task ProcessInstallUninstallEvents(bool showEmptyMessage = true);
    }
}
