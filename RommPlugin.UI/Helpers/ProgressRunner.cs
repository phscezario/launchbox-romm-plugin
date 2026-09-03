using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.Core.Interfaces;
using RommPlugin.Core.Locale;
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
                using (var form = new ProgressForm())
                {
                    var reporter = new ProgressFormReporter(form, cts.Token);

                    form.FormClosing += (_, __) =>
                    {
                        cts.Cancel();
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
                            using (var form = new ConfirmForm(
                                ex.ToString(),
                                null))
                            {
                                form.ShowDialog();
                            }
                        }
                        finally
                        {
                            try { if (!form.IsDisposed) form.Close(); } catch { }
                            tcs.TrySetResult(null);
                        }
                    };

                    Application.Run(form);
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();

            return tcs.Task;
        }
    }
}
