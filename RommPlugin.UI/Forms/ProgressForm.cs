using System;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.UI.Helpers;

namespace RommPlugin.UI.Forms
{
    /// <summary>
    /// A modal form that displays progress information including a title, status message, and progress bar.
    /// </summary>
    public partial class ProgressForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressForm"/> class.
        /// </summary>
        public ProgressForm()
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("progress.title");
            lblStatus.Text = LocaleManager.Get("progress.loading");
        }

        /// <summary>
        /// Sets the title text of the progress form window.
        /// </summary>
        /// <param name="title">The title text to display.</param>
        public void SetTitle(string title)
        {
            if (InvokeRequired && !IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() => Text = title));
            }
            else if (!IsDisposed && !InvokeRequired)
            {
                Text = title;
            }  
        }

        /// <summary>
        /// Sets the status message displayed on the progress form.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        public void SetStatus(string message)
        {
            if (InvokeRequired && !IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() => lblStatus.Text = message));
            }   
            else if (!IsDisposed && !InvokeRequired)
            {
                lblStatus.Text = message;
            }
        }

        /// <summary>
        /// Sets the progress bar value.
        /// </summary>
        /// <param name="value">The progress value (0-100).</param>
        public void SetProgress(int value)
        {
            if (InvokeRequired && !IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() => progressBar.Value = value));
            }
            else if (!IsDisposed && !InvokeRequired)
            {
                progressBar.Value = value;
            }  
        }

        /// <summary>
        /// Sets whether the progress bar is in indeterminate (marquee) mode.
        /// </summary>
        /// <param name="value">If <c>true</c>, the progress bar animates continuously; otherwise it displays a fixed value.</param>
        public void SetIndeterminate(bool value)
        {
            if (InvokeRequired && !IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() => progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous));
            }
            else if (!IsDisposed && !InvokeRequired)
            {
                progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            }
        }


    }
}
