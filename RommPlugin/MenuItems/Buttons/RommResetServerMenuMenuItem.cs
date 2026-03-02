using System;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommResetServerMenuMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static readonly RommSyncService sync = new RommSyncService();

        public override string Caption => "RomM: Remove all server metadata";

        public override async void OnSelected()
        {
            using (var form = new RommAdversityLoginForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var username = form.Username;
                    var password = form.Password;

                    var settings = RommPluginStorage.Load();

                    if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                    {
                        System.Windows.MessageBox.Show(
                            "RomM is not configured yet.",
                            "RomM Plugin"
                        );
                        return;
                    }

                    var api = new RommApiClient(settings.RommBaseUrl);
                    sync.SetApi(api);

                    try
                    {
                        await sync.RemoveAllGamesServerMetadata(username, password);

                        MessageBox.Show("All metadata has been deleted from RomM server");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("[RommPlugin] error: " + ex);
                    }
                }
            }
        }
    }

}
