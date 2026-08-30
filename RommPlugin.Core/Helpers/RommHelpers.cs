using System;
using System.IO;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Helpers
{
    public static class RommHelpers
    {
        public static string GetLaunchBoxRoot()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var level1 = Path.GetDirectoryName(assemblyPath);
            var level2 = Path.GetDirectoryName(level1);
            var level3 = Path.GetDirectoryName(level2);

            return level3;
        }

        public static string GetLaunchBoxImagesFolder()
        {
            var root = GetLaunchBoxRoot();
            var images = Path.Combine(root, "Images");
            return images;
        }
    }
}
