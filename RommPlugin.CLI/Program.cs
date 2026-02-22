using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using RommPlugin.UI.Helpers;

namespace RommPlugin.CLI
{
    internal static class Program
    {
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2) 
            {
                return 1;
            }  

            var command = args[0].ToLowerInvariant();

            if (!int.TryParse(args[1], out var rommGameId))
            {
                return 2;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(baseDir, "settings.json");

            if (!File.Exists(settingsPath))
            {
                MessageBox.Show(
                    $"Settings file not found at:\n{settingsPath}",
                    "RomM CLI",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return 3;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int exitCode = 0;

            await ProgressRunner.RunAsync(
                "Starting RomM plugin...",
                async progress =>
                {
                    try
                    {
                        progress.SetIndeterminate(true);

                        var installer = new RommInstaller(progress, settingsPath);

                        switch (command)
                        {
                            case "install":
                                progress.SetTitle("RomM - Installing game");
                                progress.SetStatus("Starting installation...");
                                await installer.InstallGameAsync(rommGameId);
                                break;

                            case "uninstall":
                                progress.SetTitle("RomM - Uninstalling game");
                                progress.SetStatus("Starting uninstallation...");
                                await installer.UninstallGameAsync(rommGameId);
                                break;

                            default:
                                exitCode = 4;
                                MessageBox.Show(
                                    $"Invalid command: {command}",
                                    "RomM CLI",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        exitCode = 5;
                        MessageBox.Show(
                            ex.ToString(),
                            "RomM CLI - Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            );

            return exitCode;
        }
    }
}