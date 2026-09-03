using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.UI.Helpers;

namespace RommPlugin.UI.Forms
{
    /// <summary>
    /// A confirmation dialog that asks the user whether to restart the application now or later.
    /// </summary>
    public partial class RestartConfirmForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartConfirmForm"/> class
        /// with the default locale message.
        /// </summary>
        public RestartConfirmForm()
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartConfirmForm"/> class
        /// with a custom message and optional custom button labels.
        /// Returns <see cref="System.Windows.Forms.DialogResult.Yes"/> for "now"
        /// and <see cref="System.Windows.Forms.DialogResult.No"/> for "later".
        /// </summary>
        /// <param name="message">Custom message to display.</param>
        /// <param name="nowText">Optional text for the "now" button. Falls back to locale.</param>
        /// <param name="laterText">Optional text for the "later" button. Falls back to locale.</param>
        /// <param name="title">Optional dialog title. Falls back to locale.</param>
        public RestartConfirmForm(string message, string nowText = null, string laterText = null, string title = null)
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();

            if (!string.IsNullOrEmpty(message))
            {
                lblMessage.Text = message;
                AutoSizeMessage();
            }
            if (!string.IsNullOrEmpty(nowText))
                btnRestartNow.Text = nowText;
            if (!string.IsNullOrEmpty(laterText))
                btnRestartLater.Text = laterText;
            if (!string.IsNullOrEmpty(title))
                Text = title;
        }

        private void AutoSizeMessage()
        {
            using (var g = CreateGraphics())
            {
                var size = g.MeasureString(lblMessage.Text, lblMessage.Font, lblMessage.Width);
                var needed = (int)System.Math.Ceiling(size.Height) + 8;
                const int minHeight = 60;
                const int maxHeight = 160;
                var clamped = System.Math.Max(minHeight, System.Math.Min(maxHeight, needed));
                var delta = clamped - lblMessage.Height;
                if (delta > 0)
                {
                    lblMessage.Height = clamped;
                    btnRestartNow.Top += delta;
                    btnRestartLater.Top += delta;
                    ClientSize = new System.Drawing.Size(ClientSize.Width, ClientSize.Height + delta);
                }
            }
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("restart.title");
            lblMessage.Text = LocaleManager.Get("restart.message");
            btnRestartNow.Text = LocaleManager.Get("restart.now");
            btnRestartLater.Text = LocaleManager.Get("restart.later");
        }


    }
}
