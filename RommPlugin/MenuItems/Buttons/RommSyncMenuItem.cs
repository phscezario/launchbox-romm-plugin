using System;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommSyncMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private RommSyncService sync = new RommSyncService();

        public override string Caption => LocaleManager.Get("menu.sync");

        public override async void OnSelected()
        {
            RommLogger.Log("[DIAG] RommSyncMenuItem.OnSelected: sync button clicked");
            try
            {
                var settings = RommPluginStorage.Load();
                RommLogger.Log($"[DIAG] RommSyncMenuItem: baseUrl={settings.RommBaseUrl}");

                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    RommLogger.Log("[DIAG] RommSyncMenuItem: baseUrl not configured");
                    MessageBox.Show(
                        LocaleManager.Get("error.not_configured"),
                        LocaleManager.Get("settings.title_box")
                    );
                    return;
                }

                RommLogger.Log("[DIAG] RommSyncMenuItem: starting sync");
                using (var api = new RommApiClient(settings.RommBaseUrl))
                {
                    sync.SetApi(api);
                    await sync.SyncAsync();
                }
                RommLogger.Log("[DIAG] RommSyncMenuItem: sync completed");
            }
            catch (Exception ex)
            {
                RommLogger.Log($"[DIAG] RommSyncMenuItem: EXCEPTION - {ex.Message}");
                RommLogger.LogError("[RommPlugin] sync error: " + ex);
                MessageBox.Show(
                    ex.Message,
                    LocaleManager.Get("progress.error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
