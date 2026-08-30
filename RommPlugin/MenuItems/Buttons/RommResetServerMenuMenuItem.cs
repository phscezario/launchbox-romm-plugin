using System;
using System.Threading;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommResetServerMenuMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static int _isRunning = 0;

        public override string Caption => LocaleManager.Get("menu.reset_server");

        public override async void OnSelected()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                MessageBox.Show(
                    LocaleManager.Get("sync.already_running"),
                    RommConstants.RootCategoryName);
                return;
            }

            try
            {
                var settings = RommPluginStorage.Load();

                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    System.Windows.MessageBox.Show(
                        LocaleManager.Get("error.not_configured"),
                        LocaleManager.Get("settings.title_box")
                    );
                    return;
                }

                var api = (RommApiClient)ServiceLocator.GetService<IRommApiClient>();
                api.ApplyAuthentication(settings);
                var sync = ServiceLocator.GetService<IRommResetServerService>();
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
            catch (Exception ex)
            {
                RommLogger.LogError("[RommPlugin] reset server unexpected error: " + ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }
    }
}
