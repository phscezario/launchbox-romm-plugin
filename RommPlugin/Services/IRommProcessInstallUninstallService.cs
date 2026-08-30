using System.Threading.Tasks;

namespace RommPlugin.Services
{
    public interface IRommProcessInstallUninstallService
    {
        Task ProcessInstallUninstallEvents(bool showEmptyMessage = true);
    }
}
