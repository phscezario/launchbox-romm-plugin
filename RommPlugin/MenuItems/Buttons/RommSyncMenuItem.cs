using System;
using System.Windows;
using RommPlugin.ApiClient;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;
using RommPlugin.Core.Storage;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommSyncMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private RommSyncService sync = new RommSyncService();

        public override string Caption => "RomM: Sync roms list from server";

        public override async void OnSelected()
        {
            try
            {
                var settings = RommPluginStorage.Load();

                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    MessageBox.Show(
                        "RomM is not configured yet.",
                        "Romm Plugin"
                    );
                    return;
                }

                var api = new RommApiClient(settings.RommBaseUrl);
                sync.SetApi(api);

                await sync.SyncAsync(settings.Username, settings.Password);

                MessageBox.Show("RomM sync completed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Romm Plugin Error");
            }
        }
    }
}
