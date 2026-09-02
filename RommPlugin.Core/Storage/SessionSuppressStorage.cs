using System;
using System.IO;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Storage
{
    /// <summary>
    /// Manages session-level suppression flags stored in session_suppress.json.
    /// Used to suppress one-time notifications (e.g., "pending installs") for the current session.
    /// </summary>
    public static class SessionSuppressStorage
    {
        private static readonly string FilePath = Path.Combine(RommPaths.PluginFolder, "session_suppress.json");

        /// <summary>
        /// Deletes the session suppress file, clearing all session-level suppression flags.
        /// Called on LaunchBox startup to reset suppressions from the previous session.
        /// </summary>
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
