using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;

namespace RommPlugin.UI.Forms
{
    public partial class ConfirmForm : Form
    {
        public bool SuppressChecked => chkSuppress.Checked;

        public ConfirmForm(string message, string checkboxText)
        {
            InitializeComponent();
            LoadIcon();
            ApplyLocale();

            lblMessage.Text = message;
            chkSuppress.Text = checkboxText;
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("confirm.title");
            btnOk.Text = LocaleManager.Get("confirm.ok");
        }

        private void LoadIcon()
        {
            try
            {
                var iconPath = Path.Combine(RommPaths.ImagesFolder, "ico.ico");
                RommLogger.Log($"[DIAG] ConfirmForm.LoadIcon: trying {iconPath}, exists={File.Exists(iconPath)}");
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Images", "ico.ico");
                    RommLogger.Log($"[DIAG] ConfirmForm.LoadIcon: fallback {iconPath}, exists={File.Exists(iconPath)}");
                }
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex) { RommLogger.Log($"[DIAG] ConfirmForm.LoadIcon: EXCEPTION - {ex.Message}"); }
        }
    }
}
