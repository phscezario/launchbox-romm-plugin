using System;
using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Storage
{
    /// <summary>
    /// Handles loading and saving of sync resume state to/from sync_information.json.
    /// Used to resume interrupted syncs from where they left off.
    /// </summary>
    public static class RommSyncInformationStorage
    {
        private static readonly string FilePath = Path.Combine(RommPaths.PluginFolder, RommConstants.SyncInformationFile);

        /// <summary>
        /// Loads the sync resume state from disk.
        /// Returns a new empty instance if the file doesn't exist or cannot be read.
        /// </summary>
        /// <returns>The deserialized <see cref="RommSyncInformation"/> instance.</returns>
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

        /// <summary>
        /// Saves the sync resume state to disk using atomic file write.
        /// </summary>
        /// <param name="syncInfo">The sync information to save.</param>
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
