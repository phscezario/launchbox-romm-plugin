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
                RommLogger.Log($"[DIAG] SessionSuppressStorage.Delete: path={FilePath}, exists={File.Exists(FilePath)}");
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    RommLogger.Log("[DIAG] SessionSuppressStorage.Delete: deleted");
                }
            }
            catch (Exception ex)
            {
                RommLogger.Log($"[DIAG] SessionSuppressStorage.Delete: EXCEPTION - {ex.Message}");
                RommLogger.LogError($"Failed to delete session suppress file: {ex.Message}");
            }
        }
    }
}
