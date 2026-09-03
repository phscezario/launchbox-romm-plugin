using System;
using System.Threading;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    /// <summary>
    /// Menu item that resets server-side metadata for all games, removing plugin-created fields.
    /// </summary>
    public class RommResetServerMenuMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static int _isRunning = 0;

        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("menu.reset_server");

        /// <inheritdoc/>
        public override async void OnSelected()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                using (var form = new ConfirmForm(LocaleManager.Get("sync.already_running")))
                {
                    form.ShowDialog();
                }
                return;
            }

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
                    using (var form = new ConfirmForm(ex.Message))
                    {
                        form.ShowDialog();
                    }
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
