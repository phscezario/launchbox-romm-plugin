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

        public ConfirmForm(string message, string checkboxText = null)
        {
            InitializeComponent();
            LoadIcon();
            ApplyLocale();

            txtMessage.Text = message;

            if (string.IsNullOrEmpty(checkboxText))
            {
                chkSuppress.Visible = false;
            }
            else
            {
                chkSuppress.Text = checkboxText;
            }

            AutoSizeForm(message);
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("confirm.title");
            btnOk.Text = LocaleManager.Get("confirm.ok");
            btnCancel.Text = LocaleManager.Get("confirm.cancel");
        }

        private void AutoSizeForm(string message)
        {
            using (var g = CreateGraphics())
            {
                var textSize = g.MeasureString(message, txtMessage.Font, txtMessage.Width);
                var lineCount = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
                var displayLines = Math.Max(lineCount, (int)Math.Ceiling(textSize.Height / txtMessage.Font.GetHeight(g)));

                var minLines = 3;
                var maxLines = 50;
                var clampedLines = Math.Max(minLines, Math.Min(maxLines, displayLines));

                var textHeight = clampedLines * (int)Math.Ceiling(txtMessage.Font.GetHeight(g));
                txtMessage.Height = textHeight;

                var formHeight = textHeight + 90;
                ClientSize = new Size(ClientSize.Width, formHeight);
            }
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
            catch (Exception ex) { RommLogger.Log($"[DIAG] ConfirmForm.LoadIcon: EXCEPTION - {ex.Message}"); }
        }
    }
}
