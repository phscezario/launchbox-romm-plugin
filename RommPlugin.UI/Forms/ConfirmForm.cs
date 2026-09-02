using System;
using System.Drawing;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.UI.Helpers;

namespace RommPlugin.UI.Forms
{
    /// <summary>
    /// A modal confirmation dialog that displays a message and optional checkbox.
    /// </summary>
    public partial class ConfirmForm : Form
    {
        /// <summary>
        /// Gets a value indicating whether the "Don't ask again" checkbox is checked.
        /// </summary>
        public bool SuppressChecked => chkSuppress.Checked;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmForm"/> class.
        /// </summary>
        /// <param name="message">The message to display in the dialog.</param>
        /// <param name="checkboxText">Optional text for a suppression checkbox. If <c>null</c>, the checkbox is hidden.</param>
        public ConfirmForm(string message, string checkboxText = null)
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();
            ActiveControl = btnCancel;

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


    }
}
