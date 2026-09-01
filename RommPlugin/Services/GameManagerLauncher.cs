using System;
using System.Windows.Forms;
using RommPlugin.Core.Logging;
using RommPlugin.UI.Forms;

namespace RommPlugin.Services
{
    public static class GameManagerLauncher
    {
        private static Form _form;
        private static readonly object _lock = new object();

        public static void EnsureOpen()
        {
            lock (_lock)
            {
                if (_form != null && !_form.IsDisposed)
                {
                    return;
                }

                try
                {
                    var form = new GameManagerForm();
                    if (!form.IsInitialized)
                    {
                        form.Dispose();
                        return;
                    }

                    var sync = new RommProcessInstallUninstallService();
                    form.SetApplyPendingHandler(() => sync.ProcessInstallUninstallEvents(false));
                    form.FormClosed += (s, e) => { _form = null; };
                    _form = form;
                    _form.Show();
                }
                catch (Exception ex)
                {
                    RommLogger.LogException(ex);
                }
            }
        }
    }
}
