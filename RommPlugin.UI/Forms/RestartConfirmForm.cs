using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Helpers;

namespace RommPlugin.UI.Forms
{
    public partial class RestartConfirmForm : Form
    {
        public RestartConfirmForm()
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();
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
