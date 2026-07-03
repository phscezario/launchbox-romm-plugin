using System;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Services;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommUpdateServerMetadataMenuMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static readonly RommSyncService sync = new RommSyncService();

        public override string Caption => "RomM: Update server with local metadata";

        public override async void OnSelected()
        {
            var settings = RommPluginStorage.Load();

            if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
            {
                System.Windows.MessageBox.Show(
                    "RomM is not configured yet.",
                    "RomM Plugin"
                );
                return;
            }

            string username;
            string password;
            string clientApiToken = null;

            using (var form = new RommAdversityLoginForm(settings))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                if (form.UseConfiguredAccount)
                {
                    username = settings.Username;
                    password = settings.Password;
                    clientApiToken = settings.ClientApiToken;
                }
                else
                {
                    username = form.Username;
                    password = form.Password;
                }

                if (form.SaveAdminAccount)
                {
                    RommAdminStorage.Save(new RommAdminAccount
                    {
                        Username = username,
                        Password = password
                    });
                }
            }

            var api = new RommApiClient(settings.RommBaseUrl);
            sync.SetApi(api);

            try
            {
                await sync.UpdateServerMetadata(username, password, clientApiToken);
            }
            catch (Exception ex)
            {
                RommLogger.LogError("[RommPlugin] error: " + ex);
                throw new Exception("[RommPlugin] error: " + ex);
            }
        }
    }

}
