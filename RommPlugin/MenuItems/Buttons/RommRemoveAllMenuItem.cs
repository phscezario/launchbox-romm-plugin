using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommRemoveAllMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        public override string Caption => LocaleManager.Get("menu.remove_all");

        public override async void OnSelected()
        {
            RommLogger.Log("[DIAG] RommRemoveAllMenuItem.OnSelected: clicked");

            using (var form = new ConfirmForm(LocaleManager.Get("remove_all.confirm")))
            {
                if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    RommLogger.Log("[DIAG] RommRemoveAllMenuItem: user cancelled");
                    return;
                }
            }

            try
            {
                var baseDir = RommPaths.PluginFolder;
                var dataDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Data"));
                var cliPath = Path.Combine(baseDir, "RommPlugin.CLI.exe");
                var launchBoxExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LaunchBox.exe"));

                if (!File.Exists(cliPath))
                {
                    RommLogger.LogError($"CLI not found at {cliPath}");
                    MessageBox.Show("CLI not found", "RomM", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                RommLogger.Log($"[DIAG] RemoveAllRomm: launching CLI with --remove-all {dataDir} --restart {launchBoxExe}");
                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"--remove-all \"{dataDir}\" --restart \"{launchBoxExe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
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

                    RommLogger.Log($"[DIAG] RemoveAllRomm CLI output: {output}");
                    if (error.Length > 0)
                        RommLogger.Log($"[DIAG] RemoveAllRomm CLI error: {error}");

                    if (process.ExitCode == 0)
                    {
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"CLI exited with code {process.ExitCode}\n{error.ToString()}",
                            "RomM",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] RemoveAll error: {ex}");
                MessageBox.Show(ex.Message, "RomM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
