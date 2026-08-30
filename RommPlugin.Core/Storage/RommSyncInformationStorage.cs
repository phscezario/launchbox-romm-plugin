using System;
using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Storage
{
    public static class RommSyncInformationStorage
    {
        private static readonly string FilePath = Path.Combine(RommPaths.PluginFolder, RommConstants.SyncInformationFile);

        public static RommSyncInformation Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new RommSyncInformation();
                }

                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<RommSyncInformation>(json) ?? new RommSyncInformation();
            }
            catch
            {
                return new RommSyncInformation();
            }
        }

        public static void Save(RommSyncInformation syncInfo)
        {
            try
            {
                var json = JsonConvert.SerializeObject(syncInfo, Formatting.Indented);
                SafeFileWriter.WriteAllText(FilePath, json);
            }
            catch
            {
            }
        }
    }
}
