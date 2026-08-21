using System;
using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Storage
{
    public static class RommPluginStorage
    {
        public static RommPluginSettings Load()
        {
            try
            {
                if (!File.Exists(RommPaths.SettingsFile))
                    return new RommPluginSettings();

                var json = File.ReadAllText(RommPaths.SettingsFile);
                return JsonConvert.DeserializeObject<RommPluginSettings>(json) ?? new RommPluginSettings();
            }
            catch
            {
                return new RommPluginSettings();
            }
        }

        public static void Save(RommPluginSettings settings)
        {
            var tempPath = Path.Combine(RommPaths.PluginFolder, $"settings.{Guid.NewGuid():N}.tmp");
            try
            {
                Directory.CreateDirectory(RommPaths.PluginFolder);
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(tempPath, json);
                File.Copy(tempPath, RommPaths.SettingsFile, true);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to save settings: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }
}
