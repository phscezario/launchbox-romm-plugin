using System;
using System.Windows.Forms;

namespace RommPlugin.UI.Forms
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
        }

        public void SetTitle(string title)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Text = title));
            }
            else
            {
                Text = title;
            }  
        }

        public void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => lblStatus.Text = message));
            }   
            else
            {
                lblStatus.Text = message;
            }
        }

        public void SetProgress(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => progressBar.Value = value));
            }
            else
            {
                progressBar.Value = value;
            }  
        }

        public void SetIndeterminate(bool value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous));
            }
            else
            {
                progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            }
        }
    }
}
