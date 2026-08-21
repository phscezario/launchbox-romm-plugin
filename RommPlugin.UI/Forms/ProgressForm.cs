using System;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Storage;

namespace RommPlugin.UI.Forms
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
            LoadIcon();
            ApplyLocale();
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("progress.title");
            lblStatus.Text = LocaleManager.Get("progress.loading");
        }

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

        private void LoadIcon()
        {
            try
            {
                var iconPath = System.IO.Path.Combine(RommPaths.ImagesFolder, "ico.ico");

                if (!System.IO.File.Exists(iconPath))
                {
                    iconPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Images", "ico.ico");
                }

                if (System.IO.File.Exists(iconPath))
                {
                    Icon = new System.Drawing.Icon(iconPath);
                }
            }
            catch
            {
            }
        }
    }
}
