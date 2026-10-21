using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Cross-process single-flight guard for install processing. Two LaunchBox
    /// instances (or overlapping manual/automatic runs) must never extract the
    /// same games concurrently: locks, partial deletes and .part churn follow.
    /// A caller that cannot take the lock waits for the running pass instead of
    /// dropping its trigger — the running pass cannot see work that arrived
    /// after its snapshot, so a dropped trigger left games stuck on
    /// "Installing".
    /// </summary>
    /// <remarks>
    /// Backed by a named <see cref="Semaphore"/>, not a <see cref="Mutex"/>.
    /// Semaphores are not thread-affine, so the guard stays correct even though
    /// the protected work runs across <c>await</c> continuations (which can
    /// resume on a different thread). A mutex would leak when released from a
    /// thread that did not acquire it (the release throws and is swallowed),
    /// leaving the lock held forever and silently disabling every later install.
    /// A semaphore is not abandoned by the OS when its owner dies either, so the
    /// owning pid is recorded in a small sidecar file and reclaimed when that
    /// process is gone: a crashed instance must not lock the survivor out
    /// forever.
    /// </remarks>
    public static class InstallProcessLock
    {
        private const string SemaphoreName = "RommLaunchBoxPlugin_InstallProcessing";

        /// <summary>Serializes the dead-owner reclaim across processes.</summary>
        private const string ReclaimMutexName = "RommLaunchBoxPlugin_InstallProcessing_Reclaim";

        private static readonly string OwnerFile =
            Path.Combine(RommPaths.PluginFolder, "install-processing.owner");

        /// <summary>How often a waiting caller re-checks the lock.</summary>
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        /// <summary>Guards lazy creation of the named semaphore.</summary>
        private static readonly object InitLock = new object();

        private static Semaphore _semaphore;

        /// <summary>
        /// 1 while this process owns the lock. A fail-open acquisition (see the
        /// catch below) never sets it, so <see cref="Exit"/> cannot release a
        /// semaphore that belongs to another process.
        /// </summary>
        private static int _owned;

        /// <summary>Tries to take the processing lock without blocking.</summary>
        public static bool TryEnter()
        {
            try
            {
                EnsureSemaphore();

                if (TryAcquire())
                    return true;

                RommLogger.Log($"[RommPlugin] Install processing already running elsewhere (owner: {DescribeOwner()}), skipping overlapping run");
                return false;
            }
            catch (Exception ex)
            {
                // Fail open: a broken lock must never block installs entirely.
                RommLogger.LogError($"[RommPlugin] Install lock unavailable, proceeding: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Takes the processing lock, waiting up to <paramref name="timeout"/>
        /// for a running pass to finish instead of dropping the trigger.
        /// </summary>
        /// <remarks>
        /// Skipping a trigger is only safe when the run holding the lock knows
        /// about the same work. It cannot: the batch snapshots the state file
        /// when it starts, so an item that finished downloading while it was
        /// extracting is invisible to it. That item then stayed "Installing"
        /// with its archive intact until the manager was reopened. Waiting keeps
        /// every trigger alive; the batch that runs afterwards re-reads the
        /// shared state file and drains whatever is still pending.
        /// </remarks>
        public static bool TryEnter(TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (timeout <= TimeSpan.Zero)
                return TryEnter();

            try
            {
                EnsureSemaphore();

                if (TryAcquire())
                    return true;

                RommLogger.Log($"[RommPlugin] Install processing is running elsewhere (owner: {DescribeOwner()}); waiting up to {timeout.TotalMinutes:0.#} min for it to finish");

                var stopwatch = Stopwatch.StartNew();

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var remaining = timeout - stopwatch.Elapsed;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    var slice = remaining < PollInterval ? remaining : PollInterval;

                    if (_semaphore.WaitOne(slice))
                    {
                        MarkOwned();
                        RommLogger.Log("[RommPlugin] Install processing lock acquired after waiting");
                        return true;
                    }

                    if (ReclaimFromDeadOwner())
                    {
                        RommLogger.Log("[RommPlugin] Install processing lock reclaimed from a dead owner");
                        return true;
                    }
                }

                RommLogger.Log("[RommPlugin] Install processing lock still busy after waiting; skipping this run");
                return false;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] Install lock unavailable, proceeding: {ex.Message}");
                return true;
            }
        }

        private static void EnsureSemaphore()
        {
            if (_semaphore != null) return;

            // Two threads can enter at once (GM trigger + startup); without the
            // lock they create two wrappers for the same named semaphore.
            lock (InitLock)
            {
                if (_semaphore == null)
                    _semaphore = new Semaphore(1, 1, SemaphoreName);
            }
        }

        private static bool TryAcquire()
        {
            if (_semaphore.WaitOne(0))
            {
                MarkOwned();
                return true;
            }

            // Reclaim takes ownership itself (inside the claim mutex), so two
            // waiters cannot both end up owning the same semaphore.
            return ReclaimFromDeadOwner();
        }

        private static void MarkOwned()
        {
            Interlocked.Exchange(ref _owned, 1);
            WriteOwner();
        }

        private static string DescribeOwner()
        {
            try
            {
                if (!File.Exists(OwnerFile)) return "unknown";

                var pid = File.ReadAllText(OwnerFile).Trim();
                return string.IsNullOrEmpty(pid) ? "unknown" : "PID " + pid;
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>Releases the processing lock.</summary>
        public static void Exit()
        {
            try
            {
                // Fail-open returns true without owning anything; a blind
                // Release() there raises the count and lets a third process into
                // the extraction while the real owner is still running.
                if (Interlocked.Exchange(ref _owned, 0) == 0)
                    return;

                ClearOwnerIfOwned();
                _semaphore?.Release();
            }
            catch
            {
                // Over-release (SemaphoreFullException) or a destroyed handle:
                // nothing to do, the next TryEnter re-creates/observes the state.
            }
        }

        /// <summary>
        /// A semaphore is not abandoned when its owner exits, so a crashed
        /// instance would leave it at zero forever while any process keeps a
        /// handle. When the recorded owner process no longer exists, take the
        /// semaphore back. Anything unreadable or ambiguous counts as busy.
        /// </summary>
        private static bool ReclaimFromDeadOwner()
        {
            // Release()+WaitOne() is not atomic: two waiters polling the same dead
            // owner could each "reclaim" it and both believe they own the
            // extraction. The claim is serialized across processes, and ownership
            // is recorded before the claim is released, so the next claimant sees
            // a live pid instead of the stale one.
            Mutex claim = null;
            var held = false;
            try
            {
                claim = new Mutex(false, ReclaimMutexName);
                try
                {
                    held = claim.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // The previous claimant died mid-claim: it is ours now.
                    held = true;
                }

                if (!held)
                    return false;

                if (!File.Exists(OwnerFile))
                    return false;

                if (!int.TryParse(File.ReadAllText(OwnerFile).Trim(), out var pid) || pid <= 0)
                    return false;

                // Our own pid means another thread/flow of this process holds it.
                if (pid == Process.GetCurrentProcess().Id)
                    return false;

                var ownerAlive = false;
                try
                {
                    using (var owner = Process.GetProcessById(pid))
                        ownerAlive = !owner.HasExited;
                }
                catch (ArgumentException)
                {
                    // No such process anymore: the owner is gone.
                }
                catch (Exception)
                {
                    // Access denied or the query failed: assume it is alive and
                    // never steal a lock that may be held by a live process.
                    ownerAlive = true;
                }

                if (ownerAlive)
                    return false;

                // Re-read immediately before stealing: a process that acquired
                // the lock after our first check rewrites the sidecar right
                // after taking it.
                if (!int.TryParse(File.ReadAllText(OwnerFile).Trim(), out var stillPid) || stillPid != pid)
                    return false;

                RommLogger.Log($"[RommPlugin] Install processing lock was held by dead process {pid}, reclaiming it");

                try { _semaphore.Release(); }
                catch (SemaphoreFullException) { }

                if (!_semaphore.WaitOne(0))
                    return false;

                // Recorded while the claim is still held: a second claimant can
                // only start after this returns true.
                MarkOwned();
                return true;
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] Install lock recovery check failed, treating as busy: {ex.Message}");
                return false;
            }
            finally
            {
                if (claim != null)
                {
                    if (held)
                    {
                        try { claim.ReleaseMutex(); } catch { }
                    }

                    try { claim.Dispose(); } catch { }
                }
            }
        }

        private static bool WriteOwner()
        {
            try
            {
                Directory.CreateDirectory(RommPaths.PluginFolder);
                // Atomic: a reader that parsed a half-written pid would name a
                // dead process and reclaim a lock that is actually held.
                SafeFileWriter.WriteAllText(OwnerFile, Process.GetCurrentProcess().Id.ToString());
                return true;
            }
            catch (Exception ex)
            {
                // The lock still excludes other processes, but a sidecar left by a
                // previous owner would make a waiter believe this process is dead
                // and steal the lock. Drop it: without a sidecar nothing is
                // reclaimed, which is the safe side of that trade.
                try { File.Delete(OwnerFile); } catch { }

                RommLogger.LogError($"[RommPlugin] Could not record the install lock owner: {ex.Message}");
                return false;
            }
        }

        private static void ClearOwnerIfOwned()
        {
            try
            {
                if (!File.Exists(OwnerFile)) return;

                if (!int.TryParse(File.ReadAllText(OwnerFile).Trim(), out var pid)) return;

                // Another process may have reclaimed (our process looked dead):
                // deleting its sidecar would break the next recovery attempt.
                if (pid == Process.GetCurrentProcess().Id)
                    File.Delete(OwnerFile);
            }
            catch
            {
            }
        }
    }
}
