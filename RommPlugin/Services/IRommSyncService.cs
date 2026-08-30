using System.Threading.Tasks;
using RommPlugin.ApiClient;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public interface IRommSyncService
    {
        IRommApiClient Api { get; }
        void SetApi(IRommApiClient api);
        Task SyncAsync(bool headless = false);
        Task UpdateServerMetadata(string username, string password, string clientApiToken = null);
        void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings);
        Task PushGameMetadataAsync(IGame game, RommGame remoteGame, RommPluginSettings settings);
        Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings);
    }
}
