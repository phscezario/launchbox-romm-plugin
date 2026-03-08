using System.Threading.Tasks;
using RommPlugin.ApiClient;
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

        public async Task RemoveAllGamesServerMetadata(string username, string password)
        {
            await ProgressRunner.RunAsync(
                "Reset Metadata in RomM server...",
                async progress =>
                {
                    _api.SetBasicAuthentication(username, password);

                    var rommPlatforms = await _api.GetPlatformsAsync();

                    if (rommPlatforms == null || rommPlatforms.Count == 0)
                    {
                        return;
                    }

                    var platformsCompleted = 0;
                    var platformsTotal = rommPlatforms.Count;

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        var rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);

                        if (rommGames == null)
                        {
                            continue;
                        }

                        progress.SetTitle($"RomM: Delete all metadata");

                        var completedGames = 0;
                        var totalGames = rommGames.Count;

                        foreach (var rommGame in rommGames)
                        {
                            progress.SetStatus($"Platform: {platformsCompleted} of {platformsTotal} | Games for this platform: {completedGames} of {totalGames}");

                            await _api.RemoveGameMetadataById(rommGame.Id);

                            completedGames++;
                        }

                        platformsCompleted++;
                    }
                }
            );
        }
    }
}