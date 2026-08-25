using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;

namespace RommPlugin.UI.Forms
{
    public partial class RestartConfirmForm : Form
    {
        public RestartConfirmForm()
        {
            InitializeComponent();
            LoadIcon();
            ApplyLocale();
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("restart.title");
            lblMessage.Text = LocaleManager.Get("restart.message");
            btnRestartNow.Text = LocaleManager.Get("restart.now");
            btnRestartLater.Text = LocaleManager.Get("restart.later");
        }

        private void LoadIcon()
        {
            try
            {
                var iconPath = Path.Combine(RommPaths.ImagesFolder, "ico.ico");
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Images", "ico.ico");
                }
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex) { RommLogger.Log($"[DIAG] RestartConfirmForm.LoadIcon: EXCEPTION - {ex.Message}"); }
        }
    }
}
