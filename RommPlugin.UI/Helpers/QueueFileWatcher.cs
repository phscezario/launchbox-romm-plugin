using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RommPlugin.Core.Models;

namespace RommPlugin.UI.Helpers
{
    public class QueueFileWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _queueFilePath;
        private readonly object _processLock = new object();
        private bool _disposed;

        public event Action<QueueAction> ActionDetected;

        public QueueFileWatcher(string queueFilePath)
        {
            _queueFilePath = queueFilePath;

            var directory = Path.GetDirectoryName(queueFilePath);
            var fileName = Path.GetFileName(queueFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = false
            };

            _watcher.Changed += OnQueueFileChanged;
            _watcher.Created += OnQueueFileChanged;
        }

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private void OnQueueFileChanged(object sender, FileSystemEventArgs e)
        {
            Task.Delay(500).ContinueWith(_ => ProcessQueueFile());
        }

        private void ProcessQueueFile()
        {
            lock (_processLock)
            {
                try
                {
                    if (!File.Exists(_queueFilePath)) return;

                    var json = File.ReadAllText(_queueFilePath);
                    var actions = JsonConvert.DeserializeObject<List<QueueAction>>(json);

                    if (actions != null && actions.Count > 0)
                    {
                        foreach (var action in actions)
                        {
                            if (!string.IsNullOrEmpty(action.Action))
                            {
                                ActionDetected?.Invoke(action);
                            }
                        }

                        File.Delete(_queueFilePath);
                    }
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _watcher?.Dispose();
        }
    }
}
