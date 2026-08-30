using System.Threading.Tasks;
using RommPlugin.ApiClient;

namespace RommPlugin.Services
{
    public interface IRommResetServerService
    {
        void SetApi(IRommApiClient api);
        Task RemoveAllGamesServerMetadata(string username, string password, string clientApiToken = null);
    }
}
