using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RommPlugin.Core.Locale;

namespace RommPlugin.UI.Forms
{
    public class AboutForm : Form
    {
        private Label lblName;
        private Label lblVersion;
        private Label lblAuthor;
        private Label lblOpenSource;
        private LinkLabel lnkGitHub;
        private Label lblDescription;
        private Button btnClose;

        public AboutForm()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = LocaleManager.Get("about.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(400, 300);
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            lblName = new Label
            {
                Text = LocaleManager.Get("about.plugin_name"),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(30, 25)
            };

            lblVersion = new Label
            {
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Location = new Point(32, 65)
            };

            lblAuthor = new Label
            {
                Text = LocaleManager.Get("about.author"),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                AutoSize = true,
                Location = new Point(32, 90)
            };

            lblOpenSource = new Label
            {
                Text = LocaleManager.Get("about.open_source"),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                AutoSize = true,
                Location = new Point(32, 110)
            };

            lblDescription = new Label
            {
                Text = LocaleManager.Get("about.description").Replace("\\n", "\n"),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                AutoSize = true,
                Location = new Point(32, 135)
            };

            lnkGitHub = new LinkLabel
            {
                Text = LocaleManager.Get("about.github"),
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(32, 200)
            };
            lnkGitHub.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/phscezario/launchbox-romm-plugin",
                        UseShellExecute = true
                    });
                }
                catch { /* browser open is best-effort */ }
            };

            btnClose = new Button
            {
                Text = LocaleManager.Get("about.close"),
                Size = new Size(100, 35),
                Location = new Point(285, 230),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            Controls.Add(lblName);
            Controls.Add(lblVersion);
            Controls.Add(lblAuthor);
            Controls.Add(lblOpenSource);
            Controls.Add(lblDescription);
            Controls.Add(lnkGitHub);
            Controls.Add(btnClose);

            AcceptButton = btnClose;
            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadVersionInfo()
        {
            lblVersion.Text = string.Format(LocaleManager.Get("about.version"), GetVersion());
            this.Text = LocaleManager.Get("about.title") + " v" + GetVersion();
        }

        private string GetVersion()
        {
            var ver = Assembly.GetEntryAssembly()?.GetName().Version
                      ?? Assembly.GetExecutingAssembly().GetName().Version;
            return ver.Major + "." + ver.Minor + "." + ver.Build;
        }
    }
}
