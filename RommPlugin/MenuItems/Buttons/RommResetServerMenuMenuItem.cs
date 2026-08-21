using System;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommResetServerMenuMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static readonly RommResetServerService sync = new RommResetServerService();

        public override string Caption => LocaleManager.Get("menu.reset_server");

        public override async void OnSelected()
        {
            RommLogger.Log("[DIAG] RommResetServerMenuMenuItem.OnSelected: clicked");
            var settings = RommPluginStorage.Load();

            if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
            {
                System.Windows.MessageBox.Show(
                    LocaleManager.Get("error.not_configured"),
                    LocaleManager.Get("settings.title_box")
                );
                return;
            }

            using (var api = new RommApiClient(settings.RommBaseUrl))
            {
                sync.SetApi(api);

                try
                {
                    await sync.RemoveAllGamesServerMetadata(
                        settings.Username,
                        settings.Password,
                        settings.ClientApiToken);

                    var syncInfo = RommSyncInformationStorage.Load();
                    syncInfo.SyncInProgress = false;
                    syncInfo.CompletedPlatformIds.Clear();
                    syncInfo.CompletedGameIdsByPlatform.Clear();
                    RommSyncInformationStorage.Save(syncInfo);
                }
                catch (Exception ex)
                {
                    RommLogger.LogError("[RommPlugin] reset server error: " + ex);
                    MessageBox.Show(
                        ex.Message,
                        LocaleManager.Get("settings.title_box"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }
    }
}
