using System;
using System.IO;
using System.Threading.Tasks;
using RommPlugin.Core.Interfaces;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Owns the plugin self-update flow (check, prompt, download, apply or
    /// defer). Pure Core logic: all user interaction goes through
    /// <see cref="IUpdatePrompts"/>, and the static update services are
    /// injectable seams so the flow is unit-testable without network or UI.
    /// Used by both the automatic startup check and the manual Settings check.
    /// </summary>
    public class PluginUpdateOrchestrator
    {
        private readonly IUpdatePrompts _prompts;
        private readonly Func<Task<UpdateCheckResult>> _checkForUpdateAsync;
        private readonly Func<bool> _hasPendingUpdate;
        private readonly Func<string> _getPendingVersion;
        private readonly Func<bool> _applyPendingUpdate;
        private readonly Func<Version> _getCurrentVersion;
        private readonly Action _cleanupUpdateDir;
        private readonly Func<bool> _hasFailedMarker;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUpdateOrchestrator"/> class.
        /// </summary>
        /// <param name="prompts">UI implementation of the update dialogs.</param>
        /// <param name="checkForUpdateAsync">Version check. Defaults to <see cref="GitHubUpdateService.CheckForUpdateAsync"/>.</param>
        /// <param name="hasPendingUpdate">Pending detection. Defaults to <see cref="GitHubUpdateService.HasPendingUpdate"/>.</param>
        /// <param name="getPendingVersion">Pending version lookup. Defaults to <see cref="GitHubUpdateService.GetPendingVersion"/>.</param>
        /// <param name="applyPendingUpdate">Update applier. Defaults to <see cref="GitHubUpdateService.ApplyPendingUpdate"/>.</param>
        /// <param name="getCurrentVersion">Installed version lookup. Defaults to <see cref="GitHubUpdateService.GetCurrentVersion"/>.</param>
        /// <param name="cleanupUpdateDir">Staging cleanup. Defaults to <see cref="GitHubUpdateService.CleanupUpdateDir"/>.</param>
        /// <param name="hasFailedMarker">Failure marker detection. Defaults to checking the staging directory.</param>
        public PluginUpdateOrchestrator(
            IUpdatePrompts prompts,
            Func<Task<UpdateCheckResult>> checkForUpdateAsync = null,
            Func<bool> hasPendingUpdate = null,
            Func<string> getPendingVersion = null,
            Func<bool> applyPendingUpdate = null,
            Func<Version> getCurrentVersion = null,
            Action cleanupUpdateDir = null,
            Func<bool> hasFailedMarker = null)
        {
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            _checkForUpdateAsync = checkForUpdateAsync ?? GitHubUpdateService.CheckForUpdateAsync;
            _hasPendingUpdate = hasPendingUpdate ?? GitHubUpdateService.HasPendingUpdate;
            _getPendingVersion = getPendingVersion ?? GitHubUpdateService.GetPendingVersion;
            _applyPendingUpdate = applyPendingUpdate ?? GitHubUpdateService.ApplyPendingUpdate;
            _getCurrentVersion = getCurrentVersion ?? GitHubUpdateService.GetCurrentVersion;
            _cleanupUpdateDir = cleanupUpdateDir ?? GitHubUpdateService.CleanupUpdateDir;
            _hasFailedMarker = hasFailedMarker ?? (() => File.Exists(UpdateInstaller.FailedMarkerPath));
        }

        /// <summary>
        /// Handles a pending (already downloaded) update on LaunchBox startup.
        /// A previous failed attempt is reported once and cleaned up, and a
        /// stale pending (not newer than installed, e.g. after a manual update)
        /// is cleaned up silently so the prompt loop ends.
        /// </summary>
        /// <returns><c>true</c> if a pending update was found (and handled); otherwise, <c>false</c>.</returns>
        public bool HandlePendingOnStartup()
        {
            try
            {
                if (_hasFailedMarker())
                {
                    RommLogger.Log("Previous update attempt failed. Showing one-shot info and cleaning up.");
                    _prompts.ShowInfo(LocaleManager.Get("update.apply_failed"));
                    _cleanupUpdateDir();
                    return true;
                }

                if (!_hasPendingUpdate())
                    return false;

                var version = _getPendingVersion() ?? "?";

                if (IsStalePending(version))
                {
                    RommLogger.Log($"Pending update {version} is not newer than installed. Cleaning up without prompting.");
                    _cleanupUpdateDir();
                    return true;
                }
                var message = string.Format(LocaleManager.Get("update.pending_message"), version);

                if (_prompts.ConfirmUpdateNow(
                    message,
                    LocaleManager.Get("update.pending_title"),
                    LocaleManager.Get("update.restart_now"),
                    LocaleManager.Get("update.restart_later")))
                {
                    RommLogger.Log("Applying pending update: " + version);
                    if (!_applyPendingUpdate())
                        _prompts.ShowInfo(LocaleManager.Get("update.apply_failed"));
                }
                else
                {
                    RommLogger.Log("User deferred pending update. Will ask again on next startup.");
                }

                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError("Failed to handle pending update: " + ex.Message);
                _cleanupUpdateDir();
                return true;
            }
        }

        /// <summary>
        /// Determines whether a pending update version is stale, i.e. not newer
        /// than the installed version (e.g. the user already updated manually).
        /// Unparseable versions are never considered stale.
        /// </summary>
        private bool IsStalePending(string pendingVersion)
        {
            try
            {
                Version pending;
                if (!Version.TryParse(pendingVersion, out pending))
                    return false;

                var current = _getCurrentVersion();
                if (current == null)
                    return false;

                return pending <= current;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Automatic update check run on LaunchBox startup. Silent when there is
        /// nothing to do; prompts only when a newer version is found.
        /// </summary>
        public async Task CheckAndPromptOnStartupAsync()
        {
            try
            {
                var result = await _checkForUpdateAsync().ConfigureAwait(true);

                if (!result.UpdateAvailable)
                    return;

                await PromptDownloadAndApplyAsync(result, quietNoAsset: true).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                RommLogger.LogError("Update check failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Manual update check (Settings "Check for Updates" button). Always
        /// reports the outcome, including "already on latest".
        /// </summary>
        public async Task RunManualCheckAsync()
        {
            try
            {
                var result = await _checkForUpdateAsync().ConfigureAwait(true);

                if (!result.UpdateAvailable)
                {
                    _prompts.ShowInfo(string.Format(
                        LocaleManager.Get("update.current_version"),
                        result.CurrentVersion != null ? result.CurrentVersion.ToString(3) : "?"));
                    return;
                }

                await PromptDownloadAndApplyAsync(result, quietNoAsset: false).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                RommLogger.LogError("Manual update check failed: " + ex.Message);
                _prompts.ShowInfo(LocaleManager.Get("update.download_failed") + ": " + ex.Message);
            }
        }

        private async Task PromptDownloadAndApplyAsync(UpdateCheckResult result, bool quietNoAsset)
        {
            var version = result.LatestVersion != null ? result.LatestVersion.ToString(3) : "?";
            var currentVersion = result.CurrentVersion != null ? result.CurrentVersion.ToString(3) : "?";

            if (result.ZipAsset == null)
            {
                RommLogger.Log("No ZIP asset found for update " + version);
                if (!quietNoAsset)
                    _prompts.ShowInfo(LocaleManager.Get("update.no_asset"));
                return;
            }

            var message = string.Format(LocaleManager.Get("update.available"), version, currentVersion);

            if (!string.IsNullOrEmpty(result.ReleaseNotes))
            {
                var notes = result.ReleaseNotes.Length > 500
                    ? result.ReleaseNotes.Substring(0, 500) + "..."
                    : result.ReleaseNotes;
                message += string.Format(LocaleManager.Get("update.release_notes"), notes);
            }

            if (!_prompts.ConfirmUpdateNow(
                message,
                LocaleManager.Get("update.title"),
                LocaleManager.Get("update.restart_now"),
                LocaleManager.Get("update.restart_later")))
            {
                RommLogger.Log("User chose to be reminded of update " + version + " on next launch.");
                return;
            }

            RommLogger.Log("Downloading update: " + result.ZipAsset.Name);
            var downloaded = await _prompts.DownloadWithProgressAsync(result.ZipAsset, version).ConfigureAwait(true);

            if (!downloaded)
            {
                _prompts.ShowInfo(LocaleManager.Get("update.download_failed"));
                return;
            }

            var applyMessage = string.Format(LocaleManager.Get("update.downloaded"), version);

            if (_prompts.ConfirmUpdateNow(
                applyMessage,
                LocaleManager.Get("update.downloaded_title"),
                LocaleManager.Get("update.restart_now"),
                LocaleManager.Get("update.restart_later")))
            {
                RommLogger.Log("User chose to apply update now: " + version);
                if (!_applyPendingUpdate())
                    _prompts.ShowInfo(LocaleManager.Get("update.apply_failed"));
            }
            else
            {
                RommLogger.Log("User chose to apply update " + version + " on next startup.");
            }
        }
    }
}
