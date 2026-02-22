using System;
using System.IO;
using System.Windows.Forms;
using RommPlugin.Core.Config;
using RommPlugin.Core.Storage;

namespace RommPlugin.UI.Forms
{
    public partial class RommSettingsForm : Form
    {
        public RommSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = RommPluginStorage.Load();

            txtBaseUrl.Text = settings.RommBaseUrl;
            txtUsername.Text = settings.Username;
            txtPassword.Text = settings.Password;
            txtRomsPath.Text = settings.RomsPath;
        }

        private void RommSettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show("Base URL is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Password is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRomsPath.Text) ||
                !Directory.Exists(txtRomsPath.Text))
            {
                MessageBox.Show("Please select a valid ROMs path.");
                return;
            }

            var settings = new RommPluginSettings
            {
                RommBaseUrl = txtBaseUrl.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text,
                RomsPath = txtRomsPath.Text
            };

            RommPluginStorage.Save(settings);

            MessageBox.Show(
                "Settings saved successfully.",
                "RomM Plugin",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            Close();
        }

        private void btnBrowseRomsPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder where ROMs will be stored";

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
    }
}
