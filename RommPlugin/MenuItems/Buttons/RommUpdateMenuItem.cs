using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.ApiClient;
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
    public class RommUpdateMenuItem : RommMenuItem, IGameMenuItemPlugin
    {
        public override string Caption => LocaleManager.Get("context.update_metadata");

        public bool SupportsMultipleGames => false;

        public bool GetIsValidForGame(IGame selectedGame)
        {
            return RommGameHelpers.TryGetRommId(selectedGame, out _);
        }

        public override void OnSelected()
        {
        }

        public async void OnSelected(IGame selectedGame)
        {
            RommLogger.Log($"[DIAG] RommUpdateMenuItem.OnSelected: game={selectedGame?.Title}");
            if (!RommGameHelpers.TryGetRommId(selectedGame, out var rommId))
            {
                RommLogger.Log("[DIAG] RommUpdateMenuItem.OnSelected: no rommId");
                return;
            }
            RommLogger.Log($"[DIAG] RommUpdateMenuItem.OnSelected: rommId={rommId}");

            var settings = RommPluginStorage.Load();
            if (string.IsNullOrWhiteSpace(settings.RommBaseUrl))
            {
                MessageBox.Show(LocaleManager.Get("error.settings_not_configured"),
                    LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var client = new RommApiClient(settings.RommBaseUrl))
                {
                    client.ApplyAuthentication(settings);

                    var rommGame = await client.GetGameByIdAsync(rommId);

                    if (rommGame == null)
                    {
                        MessageBox.Show(
                            string.Format(LocaleManager.Get("error.game_not_found"), rommId),
                            LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var syncService = new RommSyncService();
                    syncService.SetApi(client);

                    if (settings.KeepLocalData)
                    {
                        if (settings.IsAdmin)
                        {
                            await syncService.PushGameMetadataAsyncPublic(selectedGame, rommGame, settings);

                            SetCustomField(selectedGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                            SetCustomField(selectedGame, GameCustomFields.LocalMetadataHash,
                                RommMetadataComparer.ComputeLocalMetadataHash(selectedGame));
                            SetCustomField(selectedGame, GameCustomFields.RemoteMetadataHash,
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
                        syncService.ApplyServerMetadataPublic(selectedGame, rommGame, settings);
                        await syncService.SyncScreenshotsBidirectionalPublic(selectedGame, rommGame, settings);

                        SetCustomField(selectedGame, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
                        SetCustomField(selectedGame, GameCustomFields.LocalMetadataHash,
                            RommMetadataComparer.ComputeLocalMetadataHash(selectedGame));
                        SetCustomField(selectedGame, GameCustomFields.RemoteMetadataHash,
                            RommMetadataComparer.ComputeRemoteMetadataHash(rommGame));

                        PluginHelper.DataManager.Save();
                        MessageBox.Show(LocaleManager.Get("progress.finished"),
                            LocaleManager.Get("confirm.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    LocaleManager.Get("progress.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetCustomField(IGame game, string name, string value)
        {
            var field = game.GetAllCustomFields().FirstOrDefault(f => f.Name == name);
            if (field == null)
            {
                field = game.AddNewCustomField();
                field.Name = name;
                field.Value = value;
            }
            else
            {
                field.Value = value;
            }
        }

        public bool GetIsValidForGames(IGame[] selectedGames)
        {
            return false;
        }

        public void OnSelected(IGame[] selectedGames)
        {
        }
    }
}
