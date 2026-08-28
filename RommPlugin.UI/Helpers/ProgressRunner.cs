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
    public static class ProgressRunner
    {
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
                            MessageBox.Show(
                                ex.ToString(),
                                LocaleManager.Get("progress.error"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
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
