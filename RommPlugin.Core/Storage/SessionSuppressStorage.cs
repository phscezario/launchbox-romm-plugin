using System;
using System.IO;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Storage
{
    public static class SessionSuppressStorage
    {
        private static readonly string FilePath = Path.Combine(RommPaths.PluginFolder, "session_suppress.json");

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to delete session suppress file: {ex.Message}");
            }
        }
    }
}
