using System;
using System.IO;
using RommPlugin.Core.Constants;

namespace RommPlugin.Core.Storage
{
    public static class RommPaths
    {
        private const string PluginFolderName = "RomM LaunchBox Integration";

        public static readonly string PluginFolder = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "Plugins", PluginFolderName));

        public static readonly string LocalesFolder = Path.Combine(PluginFolder, "Locales");
        public static readonly string ImagesFolder = Path.Combine(PluginFolder, "Images");
        public static readonly string LogsFolder = Path.Combine(PluginFolder, "Logs");

        public static string DownloadStateFile => Path.Combine(PluginFolder, RommConstants.DownloadStateFile);
        public static string SettingsFile => Path.Combine(PluginFolder, "settings.json");
    }
}
