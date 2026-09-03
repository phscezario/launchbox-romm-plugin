using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    /// <summary>
    /// Menu item that removes all plugin data and restarts LaunchBox using the CLI tool.
    /// </summary>
    public class RommRemoveAllMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("menu.remove_all");

        /// <inheritdoc/>
        public override async void OnSelected()
        {
            try
            {
                using (var form = new ConfirmForm(LocaleManager.Get("remove_all.confirm")))
                {
                    if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }
                }

                var baseDir = RommPaths.PluginFolder;
                var dataDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Data"));
                var cliPath = Path.Combine(baseDir, RommConstants.CliExecutable);
                var launchBoxExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LaunchBox.exe"));

                if (!File.Exists(cliPath))
                {
                    RommLogger.LogError($"CLI not found at {cliPath}");
                    MessageBox.Show("CLI not found", RommConstants.RootCategoryName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"--remove-all \"{dataDir}\" --restart \"{launchBoxExe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    var output = new StringBuilder();
                    var error = new StringBuilder();
                    var tcs = new TaskCompletionSource<bool>();

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null) output.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) error.AppendLine(e.Data);
                    };
                    process.Exited += (s, e) => tcs.TrySetResult(true);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await tcs.Task;

                    if (process.ExitCode == 0)
                    {
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"CLI exited with code {process.ExitCode}\n{error.ToString()}",
                            RommConstants.RootCategoryName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] RemoveAll error: {ex}");
                MessageBox.Show(ex.Message, RommConstants.RootCategoryName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
