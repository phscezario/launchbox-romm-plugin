using System.Threading.Tasks;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Interfaces
{
    /// <summary>
    /// UI contract for the plugin self-update flow. The Core orchestrator owns
    /// all decisions and texts; implementations only render widgets and report
    /// back. Convention: <c>true</c> means "update now", <c>false</c> means
    /// "remind on next launch".
    /// </summary>
    public interface IUpdatePrompts
    {
        /// <summary>
        /// Asks whether to download and apply now or be reminded later.
        /// </summary>
        /// <returns><c>true</c> for "update now"; <c>false</c> for "later".</returns>
        bool ConfirmUpdateNow(string message, string title, string nowText, string laterText);

        /// <summary>
        /// Shows an informational message (no decision required).
        /// </summary>
        void ShowInfo(string message);

        /// <summary>
        /// Downloads the release asset while displaying progress.
        /// </summary>
        /// <returns><c>true</c> if the download succeeded; otherwise, <c>false</c>.</returns>
        Task<bool> DownloadWithProgressAsync(GitHubReleaseAsset asset, string version);
    }
}
