using System;
using System.Diagnostics;
using System.IO;
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

        public override void OnSelected()
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
                var settings = RommPluginStorage.Load();
                var baseDir = RommPaths.PluginFolder;
                var dataDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Data"));
                var cliPath = Path.Combine(baseDir, "RommPlugin.CLI.exe");

                if (!File.Exists(cliPath))
                {
                    RommLogger.LogError($"CLI not found at {cliPath}");
                    MessageBox.Show("CLI not found", "RomM", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                RommLogger.Log($"[DIAG] RemoveAllRomm: launching CLI with --remove-all {dataDir}");
                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"--remove-all \"{dataDir}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    RommLogger.Log($"[DIAG] RemoveAllRomm CLI output: {output}");
                    if (!string.IsNullOrEmpty(error))
                        RommLogger.Log($"[DIAG] RemoveAllRomm CLI error: {error}");

                    if (process.ExitCode == 0)
                    {
                        settings.CurrentPlatforms.Clear();
                        RommPluginStorage.Save(settings);

                        var restartResult = MessageBox.Show(
                            LocaleManager.Get("restart.message"),
                            LocaleManager.Get("restart.title"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (restartResult == DialogResult.Yes)
                        {
                            var launchBoxExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LaunchBox.exe"));
                            if (File.Exists(launchBoxExe))
                            {
                                Process.Start(launchBoxExe);
                                Application.Exit();
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            $"CLI exited with code {process.ExitCode}\n{error}",
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
