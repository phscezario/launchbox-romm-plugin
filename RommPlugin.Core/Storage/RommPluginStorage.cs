using System;
using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Helpers;
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
                var settings = JsonConvert.DeserializeObject<RommPluginSettings>(json) ?? new RommPluginSettings();

                if (!string.IsNullOrEmpty(settings.Password) && SecureCredentialStorage.IsEncrypted(settings.Password))
                    settings.Password = SecureCredentialStorage.Decrypt(settings.Password);

                if (!string.IsNullOrEmpty(settings.ClientApiToken) && SecureCredentialStorage.IsEncrypted(settings.ClientApiToken))
                    settings.ClientApiToken = SecureCredentialStorage.Decrypt(settings.ClientApiToken);

                return settings;
            }
            catch
            {
                return new RommPluginSettings();
            }
        }

        public static void Save(RommPluginSettings settings)
        {
            try
            {
                var clone = new RommPluginSettings
                {
                    RommBaseUrl = settings.RommBaseUrl,
                    Username = settings.Username,
                    Password = string.IsNullOrEmpty(settings.Password) ? settings.Password : SecureCredentialStorage.Encrypt(settings.Password),
                    ClientApiToken = string.IsNullOrEmpty(settings.ClientApiToken) ? settings.ClientApiToken : SecureCredentialStorage.Encrypt(settings.ClientApiToken),
                    RomsPath = settings.RomsPath,
                    KeepLocalData = settings.KeepLocalData,
                    SaveLogs = settings.SaveLogs,
                    ProcessPendingOnStartup = settings.ProcessPendingOnStartup,
                    Language = settings.Language,
                    ForceFullResync = settings.ForceFullResync,
                    ForcePushToServer = settings.ForcePushToServer,
                    LastAutoSyncAt = settings.LastAutoSyncAt,
                    LogRetentionDays = settings.LogRetentionDays,
                    PublicScreenshots = settings.PublicScreenshots,
                    UpdateStatsOnGameLaunch = settings.UpdateStatsOnGameLaunch,
                    IsAdmin = settings.IsAdmin,
                    AutoUpdateEnabled = settings.AutoUpdateEnabled,
                    AutoSyncIntervalDays = settings.AutoSyncIntervalDays,
                    SaveBatchSize = settings.SaveBatchSize,
                    LastSelectedPlatformIds = settings.LastSelectedPlatformIds
                };

                var json = JsonConvert.SerializeObject(clone, Formatting.Indented);
                SafeFileWriter.WriteAllText(RommPaths.SettingsFile, json);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
