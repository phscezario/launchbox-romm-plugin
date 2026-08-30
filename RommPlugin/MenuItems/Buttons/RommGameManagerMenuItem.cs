using System;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Services;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommGameManagerMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static readonly RommProcessInstallUninstallService _sync = new RommProcessInstallUninstallService();
        private static Form _form;

        public override string Caption => LocaleManager.Get("menu.game_manager");

        public override void OnSelected()
        {
            OpenOrBringToFront();
        }

        public static void OpenOrBringToFront()
        {
            if (_form != null && !_form.IsDisposed)
            {
                return;
            }

            var form = new GameManagerForm();
            if (!form.IsInitialized)
            {
                form.Dispose();
                return;
            }
            form.SetApplyPendingHandler(() => _sync.ProcessInstallUninstallEvents(false));
            form.FormClosed += (s, e) => { _form = null; };
            _form = form;
            _form.Show();
        }
    }
}
