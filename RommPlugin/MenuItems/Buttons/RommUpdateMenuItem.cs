using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.ApiClient;
using RommPlugin.Core;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.Helpers;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.MenuItems.Buttons
{
    /// <summary>
    /// Context menu item that synchronizes metadata for a single game between LaunchBox and RomM.
    /// </summary>
    public class RommUpdateMenuItem : RommMenuItem, IGameMenuItemPlugin
    {
        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("context.update_metadata");

        /// <inheritdoc/>
        public bool SupportsMultipleGames => false;

        /// <inheritdoc/>
        public bool GetIsValidForGame(IGame selectedGame)
        {
            return RommGameHelpers.TryGetRommId(selectedGame, out _);
        }

        /// <inheritdoc/>
        public override void OnSelected()
        {
        }

        /// <inheritdoc/>
        public async void OnSelected(IGame selectedGame)
        {
            try
            {
                if (!RommGameHelpers.TryGetRommId(selectedGame, out var rommId))
                {
                    return;
                }

                var settings = RommPluginStorage.Load();
                if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
                {
                    MessageBox.Show(LocaleManager.Get("error.settings_not_configured"),
                        LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var client = (RommApiClient)ServiceLocator.GetService<IRommApiClient>();
                client.ApplyAuthentication(settings);

                var rommGame = await client.GetGameByIdAsync(rommId);

                if (rommGame == null)
                {
                    MessageBox.Show(
                        string.Format(LocaleManager.Get("error.game_not_found"), rommId),
                        LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var syncService = (RommSyncService)ServiceLocator.GetService<IRommSyncService>();
                syncService.SetApi(client);

                if (settings.KeepLocalData)
                {
                    if (settings.IsAdmin)
                    {
                        await syncService.PushGameMetadataAsync(selectedGame, rommGame, settings);

                        RommGameHelpers.SetCustomField(selectedGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                        RommGameHelpers.SetCustomField(selectedGame, GameCustomFields.LocalMetadataHash,
                            RommMetadataComparer.ComputeLocalMetadataHash(selectedGame));
                        RommGameHelpers.SetCustomField(selectedGame, GameCustomFields.RemoteMetadataHash,
                            RommMetadataComparer.ComputeRemoteMetadataHash(rommGame));

                        PluginHelper.DataManager.Save();
                        MessageBox.Show(LocaleManager.Get("progress.finished"),
                            LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(LocaleManager.Get("sync.update_metadata_admin_required"),
                            LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    syncService.ApplyServerMetadata(selectedGame, rommGame, settings);
                    await syncService.SyncScreenshotsBidirectional(selectedGame, rommGame, settings);

                    RommGameHelpers.SetCustomField(selectedGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                    RommGameHelpers.SetCustomField(selectedGame, GameCustomFields.LocalMetadataHash,
                        RommMetadataComparer.ComputeLocalMetadataHash(selectedGame));
                    RommGameHelpers.SetCustomField(selectedGame, GameCustomFields.RemoteMetadataHash,
                        RommMetadataComparer.ComputeRemoteMetadataHash(rommGame));

                    PluginHelper.DataManager.Save();
                    MessageBox.Show(LocaleManager.Get("progress.finished"),
                        LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError("[RommPlugin] update metadata error: " + ex);
                MessageBox.Show(ex.Message,
                    LocaleManager.Get("progress.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <inheritdoc/>
        public bool GetIsValidForGames(IGame[] selectedGames)
        {
            return false;
        }

        /// <inheritdoc/>
        public void OnSelected(IGame[] selectedGames)
        {
        }
    }
}
