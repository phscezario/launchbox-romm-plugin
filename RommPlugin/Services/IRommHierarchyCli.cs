using System.Collections.Generic;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public interface IRommHierarchyCli
    {
        void LaunchHierarchyCli(List<IPlatform> platforms, List<IGame> rommGamesOnly, Dictionary<string, string> platformCategoryMap, bool restartLaunchBox = false);
    }
}
