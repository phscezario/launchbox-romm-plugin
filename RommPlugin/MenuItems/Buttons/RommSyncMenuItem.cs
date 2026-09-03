using System;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    /// <summary>
    /// Menu item that triggers a full synchronization between LaunchBox and the RomM server.
    /// </summary>
    public class RommSyncMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("menu.sync");

        /// <inheritdoc/>
        public override async void OnSelected()
        {
            try
            {
                var settings = RommPluginStorage.Load();

                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    using (var form = new ConfirmForm(LocaleManager.Get("error.not_configured")))
                    {
                        form.ShowDialog();
                    }
                    return;
                }

                var api = (RommApiClient)ServiceLocator.GetService<IRommApiClient>();
                api.ApplyAuthentication(settings);
                var sync = ServiceLocator.GetService<IRommSyncService>();
                sync.SetApi(api);
                await sync.SyncAsync();
            }
            catch (Exception ex)
            {
                RommLogger.LogError("[RommPlugin] sync error: " + ex);
                using (var form = new ConfirmForm(ex.Message))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}
