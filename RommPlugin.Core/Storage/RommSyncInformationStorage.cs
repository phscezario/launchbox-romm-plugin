using System;
using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Storage
{
    public static class RommSyncInformationStorage
    {
        private static readonly string FilePath = Path.Combine(RommPaths.PluginFolder, "sync_information.json");

        public static RommSyncInformation Load()
        {
            try
            {
                RommLogger.Log($"[DIAG] RommSyncInformationStorage.Load: path={FilePath}, exists={File.Exists(FilePath)}");
                if (!File.Exists(FilePath))
                {
                    RommLogger.Log("[DIAG] RommSyncInformationStorage.Load: file not found, returning defaults");
                    return new RommSyncInformation();
                }

                var json = File.ReadAllText(FilePath);
                RommLogger.Log($"[DIAG] RommSyncInformationStorage.Load: read {json.Length} chars");
                return JsonConvert.DeserializeObject<RommSyncInformation>(json) ?? new RommSyncInformation();
            }
            catch
            {
                RommLogger.Log("[DIAG] RommSyncInformationStorage.Load: EXCEPTION, returning defaults");
                return new RommSyncInformation();
            }
        }

        public static void Save(RommSyncInformation syncInfo)
        {
            try
            {
                RommLogger.Log($"[DIAG] RommSyncInformationStorage.Save: folder={RommPaths.PluginFolder}");
                Directory.CreateDirectory(RommPaths.PluginFolder);
                var json = JsonConvert.SerializeObject(syncInfo, Formatting.Indented);
                var tempPath = Path.Combine(RommPaths.PluginFolder, $"sync_information.{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(tempPath, json);
                    File.Copy(tempPath, FilePath, true);
                    RommLogger.Log($"[DIAG] RommSyncInformationStorage.Save: saved to {FilePath}");
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                RommLogger.Log($"[DIAG] RommSyncInformationStorage.Save: EXCEPTION - {ex.Message}");
            }
        }
    }
}
