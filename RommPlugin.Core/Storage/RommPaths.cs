using System;
using System.IO;
using RommPlugin.Core.Constants;

namespace RommPlugin.Core.Storage
{
    /// <summary>
    /// Provides resolved file system paths for all plugin-related directories and files.
    /// Paths are relative to the LaunchBox installation directory.
    /// </summary>
    public static class RommPaths
    {
        private const string PluginFolderName = "RomM LaunchBox Integration";

        /// <summary>Full path to the plugin installation folder.</summary>
        public static readonly string PluginFolder = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "Plugins", PluginFolderName));

        /// <summary>Full path to the Locales folder containing JSON translation files.</summary>
        public static readonly string LocalesFolder = Path.Combine(PluginFolder, "Locales");

        /// <summary>Full path to the Images folder containing plugin icons.</summary>
        public static readonly string ImagesFolder = Path.Combine(PluginFolder, "Images");

        /// <summary>Full path to the Logs folder for log file storage.</summary>
        public static readonly string LogsFolder = Path.Combine(PluginFolder, "Logs");

        /// <summary>Full path to the download state persistence file.</summary>
        public static string DownloadStateFile => Path.Combine(PluginFolder, RommConstants.DownloadStateFile);

        /// <summary>Full path to the settings.json configuration file.</summary>
        public static string SettingsFile => Path.Combine(PluginFolder, "settings.json");
    }
}
