using System.Threading.Tasks;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public interface IRommScreenshotSync
    {
        Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings);
        Task DownloadAndSetCoverArt(IGame game, RommGame rommGame);
        string GetCoverImagePath(IGame game);
        bool HasAnyBoxFrontImage(IGame game);
        void DeleteGameImages(IGame game);
    }
}
