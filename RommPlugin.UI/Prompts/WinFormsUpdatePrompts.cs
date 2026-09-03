using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.Core.Interfaces;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.UI.Forms;

namespace RommPlugin.UI.Prompts
{
    /// <summary>
    /// WinForms implementation of <see cref="IUpdatePrompts"/> using the
    /// plugin's own dialogs (<see cref="RestartConfirmForm"/>,
    /// <see cref="ConfirmForm"/>, <see cref="ProgressForm"/>). Thin adapter:
    /// all flow decisions live in the Core orchestrator.
    /// </summary>
    public class WinFormsUpdatePrompts : IUpdatePrompts
    {
        /// <inheritdoc/>
        public bool ConfirmUpdateNow(string message, string title, string nowText, string laterText)
        {
            using (var form = new RestartConfirmForm(message, nowText, laterText, title))
            {
                return form.ShowDialog() == DialogResult.Yes;
            }
        }

        /// <inheritdoc/>
        public void ShowInfo(string message)
        {
            using (var form = new ConfirmForm(message))
            {
                form.ShowDialog();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DownloadWithProgressAsync(GitHubReleaseAsset asset, string version)
        {
            using (var form = new ProgressForm())
            {
                form.SetTitle(LocaleManager.Get("update.downloading_title"));
                form.SetStatus(string.Format(LocaleManager.Get("update.downloading_status"), version));
                form.SetIndeterminate(false);
                form.SetProgress(0);
                form.Show();

                try
                {
                    return await GitHubUpdateService.DownloadUpdateAsync(
                        asset,
                        version,
                        progress =>
                        {
                            try { form.SetProgress(progress); }
                            catch { }
                        }).ConfigureAwait(true);
                }
                finally
                {
                    try { form.Close(); } catch { }
                }
            }
        }
    }
}
