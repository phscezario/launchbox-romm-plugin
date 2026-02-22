using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RommPlugin.ApiClient;
using RommPlugin.Core.Config;
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
                throw new Exception("Romm settings not configured.");
            }

            _api = new RommApiClient(_settings.RommBaseUrl);
        }

        private async Task LoginAsync()
        {
            await _api.LoginAsync(_settings.Username, _settings.Password);
        }

        public async Task InstallGameAsync(int rommGameId)
        {
            await LoginAsync();

            _progress.SetStatus("Searching game info...");
            _progress.SetIndeterminate(true);

            var game = await _api.GetGameByIdAsync(rommGameId);

            var localDir = Path.Combine(
                _settings.RomsPath,
                "romm",
                game.FsPath.Replace("/", "\\")
            );

            Directory.CreateDirectory(localDir);

            var isFolderGame = game.Files.Count > 1;

            var localFile = Path.Combine(localDir, game.FsName + (isFolderGame ? ".zip" : ""));

            _progress.SetStatus($"{game.FsName} is downloading...");
            await _api.DownloadGameAsync(rommGameId, localFile);

            AppendSyncEvent("install", game.Id, game.FsName);
        }

        public async Task UninstallGameAsync(int rommGameId)
        {
            await Task.Run(async () =>
            {
                await LoginAsync();

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

                AppendSyncEvent("uninstall", game.Id, game.FsName);
            });
        }

        private void AppendSyncEvent(string action, int rommGameId, string gameName)
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

            MessageBox.Show($"{gameName} is {action}ed, you need to process pending installs");
        }
    }
}
