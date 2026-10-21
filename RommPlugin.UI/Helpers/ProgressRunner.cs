using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.Core.Interfaces;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.UI.Forms;
using RommPlugin.UI.Reporters;

namespace RommPlugin.UI.Helpers
{
    /// <summary>
    /// Provides a static method to run asynchronous work on a dedicated UI thread
    /// with a progress form for displaying status updates.
    /// </summary>
    public static class ProgressRunner
    {
        /// <summary>
        /// Executes an asynchronous operation on a dedicated STA thread with a progress form.
        /// </summary>
        /// <param name="title">The title displayed on the progress form window.</param>
        /// <param name="work">The asynchronous work to perform, receiving an <see cref="IProgressReporter"/> for progress updates.</param>
        /// <returns>A <see cref="Task"/> that completes when the work finishes or the form is closed.</returns>
        public static Task RunAsync(
            string title,
            Func<IProgressReporter, Task> work)
        {
            var tcs = new TaskCompletionSource<object>();
            var cts = new CancellationTokenSource();

            var uiThread = new Thread(() =>
            {
                try
                {
                    using (var form = new ProgressForm())
                    {
                        var reporter = new ProgressFormReporter(form, cts.Token);
                        var workCompleted = false;

                        form.FormClosing += (_, e) =>
                        {
                            // Veto the user's close and ask the work to stop; the
                            // Load handler's finally closes the form for real once
                            // the work returns. Letting the form close immediately
                            // tears down this message loop before the continuation
                            // runs, so the task below is never completed and the
                            // caller's "sync already running" guard stays latched
                            // forever (it can only be reset by a run that returns).
                            if (!workCompleted)
                            {
                                e.Cancel = true;
                                cts.Cancel();
                            }
                        };

                        form.Load += async (_, __) =>
                        {
                            try
                            {
                                form.SetTitle(title);
                                form.SetIndeterminate(true);

                                await work(reporter);
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            catch (Exception ex)
                            {
                                using (var errorForm = new ConfirmForm(
                                    ex.ToString(),
                                    null))
                                {
                                    errorForm.ShowDialog();
                                }
                            }
                            finally
                            {
                                workCompleted = true;
                                try { if (!form.IsDisposed) form.Close(); } catch { }
                                tcs.TrySetResult(null);
                            }
                        };

                        Application.Run(form);
                    }
                }
                catch (Exception ex)
                {
                    // A throwing form (constructor, paint, FormClosing) must not
                    // leave the caller awaiting forever: AutoInstallAsync would
                    // keep its "already processing" guard latched and stop
                    // installing until LaunchBox restarted.
                    RommLogger.LogError($"[Progress] Unhandled error on the progress thread: {ex}");
                }
                finally
                {
                    tcs.TrySetResult(null);
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();

            return tcs.Task;
        }
    }
}
