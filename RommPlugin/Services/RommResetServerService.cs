using System.Windows;
using System.Threading.Tasks;
using RommPlugin.ApiClient;
using RommPlugin.Core.Logging;
using RommPlugin.UI.Helpers;

namespace RommPlugin.Services
{
    public class RommResetServerService
    {
        private RommApiClient _api;

        public void SetApi(RommApiClient api)
        {
            _api = api;
        }

        public async Task RemoveAllGamesServerMetadata(string username, string password, string clientApiToken = null)
        {
            await ProgressRunner.RunAsync(
                "Reset Metadata in RomM server...",
                async progress =>
                {
                    if (!string.IsNullOrWhiteSpace(clientApiToken))
                    {
                        _api.SetBearerAuthentication(clientApiToken.Trim());
                    }
                    else
                    {
                        _api.SetBasicAuthentication(username, password);
                    }

                    var rommPlatforms = await _api.GetPlatformsAsync();

                    if (rommPlatforms == null || rommPlatforms.Count == 0)
                    {
                        return;
                    }

                    RommLogger.Log($"Reset metadata started: {rommPlatforms.Count} platforms");

                    var platformsCompleted = 0;
                    var platformsTotal = rommPlatforms.Count;

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        var rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);

                        if (rommGames == null)
                        {
                            continue;
                        }

                        RommLogger.Log($"Platform '{rommPlatform.Name}': {rommGames.Count} games to reset");

                        progress.SetTitle($"RomM: Delete all metadata");

                        var completedGames = 0;
                        var totalGames = rommGames.Count;

                        foreach (var rommGame in rommGames)
                        {
                            progress.SetStatus($"Platform: {platformsCompleted} of {platformsTotal} | Games for this platform: {completedGames} of {totalGames}");

                            await _api.RemoveGameMetadataById(rommGame.Id);

                            RommLogger.Log($"Game {rommGame.Id} metadata removed");
                            completedGames++;
                        }

                        platformsCompleted++;

                        RommLogger.Log($"Reset metadata completed for platform '{rommPlatform.Name}'");
                    }

                    RommLogger.Log("Remove all server metadata completed successfully");
                    MessageBox.Show("All metadata has been deleted from RomM server");
                }
            );
        }
    }
}