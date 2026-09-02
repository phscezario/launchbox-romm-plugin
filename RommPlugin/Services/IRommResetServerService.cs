using System.Threading.Tasks;
using RommPlugin.ApiClient;

namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for resetting (deleting) metadata on the RomM server.
    /// </summary>
    public interface IRommResetServerService
    {
        /// <summary>
        /// Sets the RomM API client instance used for server communication.
        /// </summary>
        /// <param name="api">The API client to use for RomM server requests.</param>
        void SetApi(IRommApiClient api);

        /// <summary>
        /// Removes all metadata from every game across all platforms on the RomM server.
        /// </summary>
        /// <param name="username">The RomM server username for basic authentication.</param>
        /// <param name="password">The RomM server password for basic authentication.</param>
        /// <param name="clientApiToken">Optional bearer token for authentication. If provided, username and password are ignored.</param>
        /// <returns>A task representing the asynchronous reset operation.</returns>
        Task RemoveAllGamesServerMetadata(string username, string password, string clientApiToken = null);
    }
}
