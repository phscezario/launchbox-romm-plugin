using System;
using System.Linq;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Tells how this LaunchBox process was started.
    /// <para>
    /// LaunchBox relaunches itself with <c>-recovery</c> after an unclean
    /// shutdown. Both instances load the plugin and share the same state files,
    /// so the recovery instance must stay passive: running auto sync, installs
    /// and uninstalls there races the primary instance for the same files, and
    /// a run that loses that race used to leave games stuck on "Installing"
    /// with the archive intact on disk.
    /// </para>
    /// </summary>
    public static class LaunchBoxInstance
    {
        /// <summary>Switch LaunchBox passes when it starts in recovery mode.</summary>
        public const string RecoverySwitch = "-recovery";

        /// <summary>
        /// True when the current process was started as LaunchBox's recovery
        /// instance. Never throws: an unreadable command line counts as primary.
        /// </summary>
        public static bool IsRecoveryInstance()
        {
            try
            {
                return IsRecoveryInstance(Environment.GetCommandLineArgs());
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Pure check used by tests and by the current-process overload.
        /// </summary>
        public static bool IsRecoveryInstance(string[] args)
        {
            if (args == null) return false;

            return args.Any(arg =>
            {
                if (arg == null) return false;

                var value = arg.Trim().Trim('"');
                return string.Equals(value, RecoverySwitch, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
