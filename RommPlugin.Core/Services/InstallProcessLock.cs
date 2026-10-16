using System;
using System.Threading;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Cross-process single-flight guard for install processing. Two LaunchBox
    /// instances (or overlapping manual/automatic runs) must never extract the
    /// same games concurrently: locks, partial deletes and .part churn follow.
    /// A skipped run just logs; the next trigger picks the leftovers up.
    /// </summary>
    public static class InstallProcessLock
    {
        private const string MutexName = "RommLaunchBoxPlugin_InstallProcessing";

        private static Mutex _mutex;

        /// <summary>Tries to take the processing lock without blocking.</summary>
        public static bool TryEnter()
        {
            try
            {
                if (_mutex == null)
                    _mutex = new Mutex(false, MutexName);

                bool taken;
                try
                {
                    taken = _mutex.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // Previous holder died mid-run; ownership passes to us.
                    taken = true;
                }

                if (!taken)
                    RommLogger.Log("[RommPlugin] Install processing already running elsewhere, skipping overlapping run");

                return taken;
            }
            catch (Exception ex)
            {
                // Fail open: a broken mutex must never block installs entirely.
                RommLogger.LogError($"[RommPlugin] Install lock unavailable, proceeding: {ex.Message}");
                return true;
            }
        }

        /// <summary>Releases the processing lock.</summary>
        public static void Exit()
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch
            {
            }
        }
    }
}
