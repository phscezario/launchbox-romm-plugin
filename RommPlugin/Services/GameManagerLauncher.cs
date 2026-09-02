using System;
using System.Windows.Forms;
using RommPlugin.Core.Logging;
using RommPlugin.UI.Forms;

namespace RommPlugin.Services
{
    /// <summary>
    /// Manages the singleton lifecycle of the Game Manager form, ensuring only one instance is open at a time.
    /// </summary>
    public static class GameManagerLauncher
    {
        private static Form _form;
        private static readonly object _lock = new object();

        /// <summary>
        /// Ensures the Game Manager form is open and visible. If the form is disposed or not yet created,
        /// a new instance is created. Configures the pending install handler and form close cleanup.
        /// </summary>
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
