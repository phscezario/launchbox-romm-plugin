using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Helpers;

namespace RommPlugin.UI.Forms
{
    public partial class RommSettingsForm : Form
    {
        public RommSettingsForm()
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();
            ActiveControl = btnCancel;
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("settings.title");
            label1.Text = LocaleManager.Get("settings.title");
            label2.Text = LocaleManager.Get("settings.base_url");
            label3.Text = LocaleManager.Get("settings.username");
            label4.Text = LocaleManager.Get("settings.password");
            labelToken.Text = LocaleManager.Get("settings.token");
            labelTokenHint.Text = LocaleManager.Get("settings.token_hint");
            label6.Text = LocaleManager.Get("settings.login_info");
            label5.Text = LocaleManager.Get("settings.roms_path");
            keepLocalData.Text = LocaleManager.Get("settings.keep_local");
            saveLogs.Text = LocaleManager.Get("settings.save_logs");
            processPendingOnStartup.Text = LocaleManager.Get("settings.process_on_startup");
            forceFullResync.Text = LocaleManager.Get("settings.force_full_resync");
            forceFullResync.AccessibleDescription = LocaleManager.Get("settings.force_full_resync_hint");
            publicScreenshots.Text = LocaleManager.Get("settings.public_screenshots");
            updateStatsOnLaunch.Text = LocaleManager.Get("settings.update_stats_on_launch");
            isAdmin.Text = LocaleManager.Get("settings.is_admin");
            forcePushToServer.Text = LocaleManager.Get("settings.force_push_to_server");
            forcePushToServer.AccessibleDescription = LocaleManager.Get("settings.force_push_to_server_hint");
            lblBehavior.Text = LocaleManager.Get("settings.behavior");
            lblAutoSyncInterval.Text = LocaleManager.Get("settings.auto_sync_interval");
            lblAutoSyncIntervalHint.Text = LocaleManager.Get("settings.auto_sync_interval_hint");
            lblLogRetention.Text = LocaleManager.Get("settings.log_retention_days");
            lblLogRetentionHint.Text = LocaleManager.Get("settings.log_retention_hint");
            lblSaveBatchSize.Text = LocaleManager.Get("settings.save_batch_size");
            lblSaveBatchSizeHint.Text = LocaleManager.Get("settings.save_batch_size_hint");
            lblLanguage.Text = LocaleManager.Get("settings.language");
            btnSave.Text = LocaleManager.Get("settings.save");
            btnCancel.Text = LocaleManager.Get("settings.cancel");
            btnTestConnection.Text = LocaleManager.Get("settings.test_connection");
        }

        private void LoadSettings()
        {
            RommPluginSettings settings;

            try
            {
                settings = RommPluginStorage.Load();
            }
            catch (Exception)
            {
                settings = new RommPluginSettings();
            }

            txtBaseUrl.Text = settings.RommBaseUrl;
            txtUsername.Text = settings.Username;
            txtPassword.Text = settings.Password;
            txtClientApiToken.Text = settings.ClientApiToken;
            txtRomsPath.Text = settings.RomsPath;
            keepLocalData.Checked = settings.KeepLocalData;
            saveLogs.Checked = settings.SaveLogs;
            processPendingOnStartup.Checked = settings.ProcessPendingOnStartup;
            forceFullResync.Checked = settings.ForceFullResync;
            publicScreenshots.Checked = settings.PublicScreenshots;
            updateStatsOnLaunch.Checked = settings.UpdateStatsOnGameLaunch;
            isAdmin.Checked = settings.IsAdmin;
            forcePushToServer.Checked = settings.ForcePushToServer;
            nudAutoSyncInterval.Value = Math.Max(-1, Math.Min(365, settings.AutoSyncIntervalDays));
            nudLogRetention.Value = Math.Max(1, Math.Min(365, settings.LogRetentionDays));
            nudSaveBatchSize.Value = Math.Max(1, Math.Min(500, settings.SaveBatchSize));

            var localeFolder = RommPaths.LocalesFolder;
            var languages = LocaleManager.GetAvailableLanguages(localeFolder);

            cmbLanguage.Items.Clear();
            foreach (var lang in languages)
            {
                cmbLanguage.Items.Add(lang);
            }

            var currentLang = languages.FirstOrDefault(kvp => kvp.Key == (settings.Language ?? "en"));
            if (currentLang.Key != null)
                cmbLanguage.SelectedItem = currentLang;
            else if (cmbLanguage.Items.Count > 0)
                cmbLanguage.SelectedIndex = 0;
        }

        private void RommSettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show(LocaleManager.Get("settings.base_url_required"));
                return;
            }

            var hasToken = !string.IsNullOrWhiteSpace(txtClientApiToken.Text);
            var hasUserPass = !string.IsNullOrWhiteSpace(txtUsername.Text) &&
                              !string.IsNullOrWhiteSpace(txtPassword.Text);

            if (!hasToken && !hasUserPass)
            {
                MessageBox.Show(
                    LocaleManager.Get("settings.auth_required")
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRomsPath.Text) ||
                !Directory.Exists(txtRomsPath.Text))
            {
                MessageBox.Show(LocaleManager.Get("settings.roms_path_invalid"));
                return;
            }

            if (hasToken &&
                (!string.IsNullOrWhiteSpace(txtUsername.Text) ||
                 !string.IsNullOrWhiteSpace(txtPassword.Text)))
            {
                var choice = MessageBox.Show(
                    LocaleManager.Get("settings.token_priority"),
                    LocaleManager.Get("settings.title_box"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (choice == DialogResult.Yes)
                {
                    txtUsername.Clear();
                    txtPassword.Clear();
                }
            }

            var settings = RommPluginStorage.Load();
            settings.RommBaseUrl = txtBaseUrl.Text.Trim();
            settings.Username = txtUsername.Text.Trim();
            settings.Password = txtPassword.Text;
            settings.ClientApiToken = txtClientApiToken.Text.Trim();
            settings.RomsPath = txtRomsPath.Text;
            settings.KeepLocalData = keepLocalData.Checked;
            settings.SaveLogs = saveLogs.Checked;
            settings.ProcessPendingOnStartup = processPendingOnStartup.Checked;
            settings.ForceFullResync = forceFullResync.Checked;
            settings.PublicScreenshots = publicScreenshots.Checked;
            settings.UpdateStatsOnGameLaunch = updateStatsOnLaunch.Checked;
            settings.IsAdmin = isAdmin.Checked;
            settings.ForcePushToServer = forcePushToServer.Checked;
            settings.AutoSyncIntervalDays = (int)nudAutoSyncInterval.Value;
            settings.LogRetentionDays = (int)nudLogRetention.Value;
            settings.SaveBatchSize = (int)nudSaveBatchSize.Value;

            if (cmbLanguage.SelectedItem is KeyValuePair<string, string> selectedLang)
                settings.Language = selectedLang.Key;

            RommPluginStorage.Save(settings);
            RommLogger.Initialize(settings.SaveLogs, settings.LogRetentionDays);

            var localeFolder = RommPaths.LocalesFolder;
            try
            {
                LocaleManager.Initialize(localeFolder, settings.Language ?? "en");
            }
            catch
            {
            }

            MessageBox.Show(
                LocaleManager.Get("settings.saved"),
                LocaleManager.Get("settings.title_box"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            Close();
        }

        private void btnBrowseRomsPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = LocaleManager.Get("settings.roms_folder_desc");

                if (!string.IsNullOrWhiteSpace(txtRomsPath.Text) &&
                    Directory.Exists(txtRomsPath.Text))
                {
                    dialog.SelectedPath = txtRomsPath.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtRomsPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show(LocaleManager.Get("settings.base_url_required"));
                return;
            }

            var hasToken = !string.IsNullOrWhiteSpace(txtClientApiToken.Text);
            var hasUserPass = !string.IsNullOrWhiteSpace(txtUsername.Text) &&
                              !string.IsNullOrWhiteSpace(txtPassword.Text);

            if (!hasToken && !hasUserPass)
            {
                MessageBox.Show(
                    LocaleManager.Get("settings.auth_required_test")
                );
                return;
            }

            btnTestConnection.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var tester = ServiceLocator.GetService<RommConnectionTester>();
                var result = await tester.TestAsync(
                    txtBaseUrl.Text.Trim(),
                    txtClientApiToken.Text.Trim(),
                    txtUsername.Text.Trim(),
                    txtPassword.Text
                );

                MessageBox.Show(
                    result.Message,
                    LocaleManager.Get("settings.title_box"),
                    MessageBoxButtons.OK,
                    result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocaleManager.Get("update.error_title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnTestConnection.Enabled = true;
            }
        }



        private async void btnCheckUpdates_Click(object sender, EventArgs e)
        {
            btnCheckUpdates.Enabled = false;
            btnCheckUpdates.Text = "Checking...";
            Cursor = Cursors.WaitCursor;

            try
            {
                var result = await GitHubUpdateService.CheckForUpdateAsync();

                if (!result.UpdateAvailable)
                {
                    MessageBox.Show(
                        string.Format(LocaleManager.Get("update.current_version"), result.CurrentVersion.ToString(3)),
                        LocaleManager.Get("settings.title_box"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var version = result.LatestVersion.ToString(3);
                var message = string.Format(LocaleManager.Get("update.available"), version, result.CurrentVersion.ToString(3));

                var dialogResult = MessageBox.Show(
                    message,
                    LocaleManager.Get("update.title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dialogResult != DialogResult.Yes)
                    return;

                var asset = result.ZipAsset ?? result.SetupAsset;
                if (asset == null)
                {
                    MessageBox.Show(
                        LocaleManager.Get("update.no_asset"),
                        LocaleManager.Get("update.error_title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var downloaded = await GitHubUpdateService.DownloadUpdateAsync(asset, version);
                if (!downloaded)
                {
                    MessageBox.Show(
                        LocaleManager.Get("update.download_failed"),
                        LocaleManager.Get("update.error_title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var restartResult = MessageBox.Show(
                    string.Format(LocaleManager.Get("update.downloaded"), version),
                    LocaleManager.Get("update.downloaded_title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (restartResult == DialogResult.Yes)
                {
                    GitHubUpdateService.ApplyPendingUpdate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocaleManager.Get("update.download_failed") + ": " + ex.Message,
                    LocaleManager.Get("update.error_title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnCheckUpdates.Enabled = true;
                btnCheckUpdates.Text = "Check for Updates";
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }
    }
}
