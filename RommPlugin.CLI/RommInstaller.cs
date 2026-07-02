using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RommPlugin.ApiClient;
using RommPlugin.Core.Interfaces;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;

namespace RommPlugin.CLI
{
    public class RommInstaller
    {
        private readonly RommApiClient _api;
        private readonly RommPluginSettings _settings;
        private readonly IProgressReporter _progress;

        public RommInstaller(IProgressReporter progress, string settingsPath)
        {
            _progress = progress;
            _settings = RommPluginStorage.LoadFrom(settingsPath);

            if (string.IsNullOrEmpty(_settings.RommBaseUrl))
            {
                MessageBox.Show("RomM settings not configured");
            }

            _api = new RommApiClient(_settings.RommBaseUrl);
        }

        public async Task InstallGameAsync(int rommGameId)
        {
            _api.ApplyAuthentication(_settings);

            _progress.SetStatus("Searching game info...");
            _progress.SetIndeterminate(true);

            var game = await _api.GetGameByIdAsync(rommGameId);

            var localDir = Path.Combine(
                _settings.RomsPath,
                "romm",
                game.FsPath.Replace("/", "\\")
            );

            Directory.CreateDirectory(localDir);

            var isFolderGame = game.HasMultipleFiles;

            var localFile = Path.Combine(localDir, game.FsName + (isFolderGame ? ".zip" : ""));

            if (File.Exists(localFile))
            {
                var result = MessageBox.Show(
                    $"{game.FsName} already exists. Do you want to replace it?",
                    "RomM CLI - Confirm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.No)
                {
                    return;
                }
            }

            try
            {
                _progress.SetStatus($"{game.FsName} is downloading...");
                await _api.DownloadGameAsync(rommGameId, localFile);
            }
            catch (Exception)
            {
                if (File.Exists(localFile))
                {
                    File.Delete(localFile);
                }

                throw;
            }

            AppendSyncEvent("install", game.Id, game.FsName);
        }

        public async Task UninstallGameAsync(int rommGameId)
        {
            await Task.Run(async () =>
            {
                _api.ApplyAuthentication(_settings);

                _progress.SetStatus("Searching game info...");
                _progress.SetIndeterminate(true);

                var game = await _api.GetGameByIdAsync(rommGameId);

                var localFile = Path.Combine(
                    _settings.RomsPath,
                    "romm",
                    game.FsPath.Replace("/", "\\"),
                    game.FsName
                );

                _progress.SetStatus($"{game.FsName} is deleting...");

                if (File.Exists(localFile))
                {
                    _progress.SetIndeterminate(true);
                    File.Delete(localFile);
                }
                else if (Directory.Exists(localFile))
                {
                    _progress.SetIndeterminate(true);
                    Directory.Delete(localFile, true);
                }
                else
                {
                    MessageBox.Show($"{game.FsName} was already removed");
                }

                AppendSyncEvent("uninstall", game.Id, game.FsName);
            });
        }

        private void AppendSyncEvent(string action, int rommGameId, string gameName)
        {
            const string mutexName = "Global\\RommPluginSync";

            using (var mutex = new Mutex(false, mutexName))
            {
                try
                {
                    mutex.WaitOne();
                }
                catch (AbandonedMutexException)
                {
                }

                try
                {
                    var flagPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "romm.sync"
                    );

                    RommSyncFile file;

                    if (File.Exists(flagPath))
                    {
                        var json = File.ReadAllText(flagPath);
                        file = JsonConvert.DeserializeObject<RommSyncFile>(json) ?? new RommSyncFile();
                    }
                    else
                    {
                        file = new RommSyncFile();
                    }

                    file.Events = file.Events == null ? new List<RommSyncEvent>() : file.Events;

                    var existingEvent = file.Events.FirstOrDefault(e => e.RommGameId == rommGameId);

                    if (existingEvent == null)
                    {
                        file.Events.Add(new RommSyncEvent
                        {
                            RommGameId = rommGameId,
                            Action = action,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existingEvent.Action = action;
                        existingEvent.Timestamp = DateTime.UtcNow;
                    }

                    File.WriteAllText(
                        flagPath,
                        JsonConvert.SerializeObject(file, Formatting.Indented)
                    );
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            MessageBox.Show($"{gameName} is {action}ed, you need to process pending installs");
        }
    }
}
