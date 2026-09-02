using System.IO;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Provides helper methods for resolving LaunchBox directory paths.
    /// </summary>
    public static class RommHelpers
    {
        /// <summary>
        /// Gets the root directory of the LaunchBox installation.
        /// Navigates three levels up from the executing assembly's location.
        /// </summary>
        /// <returns>The full path to the LaunchBox root directory.</returns>
        public static string GetLaunchBoxRoot()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var level1 = Path.GetDirectoryName(assemblyPath);
            var level2 = Path.GetDirectoryName(level1);
            var level3 = Path.GetDirectoryName(level2);

            return level3;
        }

        /// <summary>
        /// Gets the path to the LaunchBox Images folder.
        /// </summary>
        /// <returns>The full path to the Images folder within the LaunchBox installation.</returns>
        public static string GetLaunchBoxImagesFolder()
        {
            var root = GetLaunchBoxRoot();
            var images = Path.Combine(root, "Images");
            return images;
        }
    }
}
