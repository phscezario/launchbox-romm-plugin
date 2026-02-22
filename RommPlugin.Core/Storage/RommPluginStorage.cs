using System;
using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Config;

namespace RommPlugin.Core.Storage
{
    public static class RommPluginStorage
    {
        private static readonly string Folder =
            Path.Combine(
                 AppDomain.CurrentDomain.BaseDirectory,
                "Plugins",
                "RomM LaunchBox Integration"
            );

        private static readonly string FilePath = Path.Combine(Folder, "settings.json");

        public static RommPluginSettings Load()
        {
            if (!File.Exists(FilePath))
            {
                return new RommPluginSettings();
            }

            var json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<RommPluginSettings>(json);
        }

        public static RommPluginSettings LoadFrom(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Settings not found", filePath);
            }

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<RommPluginSettings>(json);
        }

        public static void Save(RommPluginSettings settings)
        {
            Directory.CreateDirectory(Folder);
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }
    }
}
