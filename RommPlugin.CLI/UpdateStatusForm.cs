using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace RommPlugin.CLI
{
    /// <summary>
    /// Visible status window shown while the CLI applies a plugin update, so the
    /// user can follow the progress (closing LaunchBox, copying files,
    /// restarting) instead of staring at a closed app. No buttons on purpose:
    /// closing mid-copy would corrupt the installation.
    /// Runs on its own STA thread; the worker thread reports via <see cref="Reporter"/>.
    /// </summary>
    public sealed class UpdateStatusForm : Form
    {
        private readonly Label _lblStatus;
        private readonly ProgressBar _progressBar;

        /// <summary>Thread-safe progress reporter bound to the form's UI thread.</summary>
        public sealed class Reporter
        {
            private readonly UpdateStatusForm _form;

            internal Reporter(UpdateStatusForm form)
            {
                _form = form;
            }

            /// <summary>Shows an indeterminate (marquee) bar with the given status text.</summary>
            public void Indeterminate(string status)
            {
                Set(() =>
                {
                    _form._lblStatus.Text = status;
                    _form._progressBar.Style = ProgressBarStyle.Marquee;
                });
            }

            /// <summary>Shows determinate progress (done/total) with the given status text.</summary>
            public void Progress(string status, int done, int total)
            {
                Set(() =>
                {
                    _form._lblStatus.Text = status;
                    _form._progressBar.Style = ProgressBarStyle.Continuous;
                    _form._progressBar.Maximum = Math.Max(1, total);
                    _form._progressBar.Value = Math.Max(0, Math.Min(done, total));
                });
            }

            private void Set(Action action)
            {
                try
                {
                    if (_form.IsDisposed || !_form.IsHandleCreated)
                        return;

                    if (_form.InvokeRequired)
                        _form.BeginInvoke(action);
                    else
                        action();
                }
                catch
                {
                }
            }
        }

        private UpdateStatusForm()
        {
            Text = "RomM Plugin - Aplicando atualização";
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = true;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 110);

            _lblStatus = new Label
            {
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(380, 40),
                Text = "Iniciando atualização..."
            };

            _progressBar = new ProgressBar
            {
                ForeColor = Color.Crimson,
                Location = new Point(20, 65),
                Size = new Size(380, 14),
                Style = ProgressBarStyle.Marquee
            };

            Controls.Add(_lblStatus);
            Controls.Add(_progressBar);
        }

        /// <summary>
        /// Shows the status window on a dedicated STA thread and returns a
        /// reporter plus a closer. The caller must invoke the closer when done.
        /// </summary>
        public static Tuple<Reporter, Action> ShowOnDedicatedThread()
        {
            UpdateStatusForm form = null;
            var ready = new ManualResetEventSlim(false);

            var uiThread = new Thread(() =>
            {
                try
                {
                    form = new UpdateStatusForm();
                    ready.Set();
                    Application.Run(form);
                }
                catch
                {
                    try { ready.Set(); } catch { }
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();
            ready.Wait(TimeSpan.FromSeconds(10));

            Reporter reporter = form != null ? new Reporter(form) : null;
            Action close = () =>
            {
                try
                {
                    if (form != null && !form.IsDisposed)
                    {
                        if (form.InvokeRequired)
                            form.BeginInvoke(new Action(() => { try { form.Close(); } catch { } }));
                        else
                            form.Close();
                    }
                }
                catch
                {
                }

                try { uiThread.Join(TimeSpan.FromSeconds(5)); } catch { }
            };

            return Tuple.Create(reporter, close);
        }
    }
}
