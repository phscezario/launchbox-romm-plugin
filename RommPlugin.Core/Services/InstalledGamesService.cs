using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Tracks which games have been installed locally from the RomM server,
    /// persisting records to a JSON file.
    /// </summary>
    public class InstalledGamesService : IInstalledGamesService
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private InstalledGamesFile _file;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledGamesService"/> class
        /// and loads existing records from disk.
        /// </summary>
        /// <param name="filePath">Path to the JSON file used to persist installed game records.</param>
        public InstalledGamesService(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        /// <inheritdoc/>
        public IReadOnlyList<InstalledGameRecord> GetAll()
        {
            lock (_lock)
            {
                return _file.Games.ToList();
            }
        }

        /// <inheritdoc/>
        public InstalledGameRecord GetByGameId(int rommGameId)
        {
            lock (_lock)
            {
                return _file.Games.FirstOrDefault(g => g.RommGameId == rommGameId);
            }
        }

        /// <inheritdoc/>
        public bool IsInstalled(int rommGameId)
        {
            lock (_lock)
            {
                return _file.Games.Any(g => g.RommGameId == rommGameId && !g.UninstalledAt.HasValue);
            }
        }

        /// <inheritdoc/>
        public void MarkInstalled(InstalledGameRecord record)
        {
            lock (_lock)
            {
                var existing = _file.Games.FirstOrDefault(g => g.RommGameId == record.RommGameId);

                if (existing != null)
                {
                    existing.Title = record.Title;
                    existing.Platform = record.Platform;
                    existing.Category = record.Category;
                    existing.RemotePath = record.RemotePath;
                    existing.FileName = record.FileName;
                    existing.InstalledPath = record.InstalledPath;
                    existing.InstalledAt = record.InstalledAt;
                    existing.UninstalledAt = null;
                }
                else
                {
                    _file.Games.Add(record);
                }
            }

            Save();
        }

        /// <inheritdoc/>
        public void MarkUninstalled(int rommGameId)
        {
            lock (_lock)
            {
                var record = _file.Games.FirstOrDefault(g => g.RommGameId == rommGameId);
                if (record != null)
                {
                    record.UninstalledAt = DateTime.UtcNow;
                }
            }

            Save();
        }

        /// <inheritdoc/>
        public void RemoveUninstalled()
        {
            lock (_lock)
            {
                _file.Games.RemoveAll(g => g.UninstalledAt.HasValue);
            }

            Save();
        }

        /// <summary>
        /// Saves the current installed games records to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                string json;
                lock (_lock)
                {
                    json = JsonConvert.SerializeObject(_file, Formatting.Indented);
                }

                SafeFileWriter.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] Failed to save installed games: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads installed games records from the JSON file on disk.
        /// </summary>
        public void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    lock (_lock)
                    {
                        _file = new InstalledGamesFile();
                    }
                    return;
                }

                var json = File.ReadAllText(_filePath);
                var file = JsonConvert.DeserializeObject<InstalledGamesFile>(json);

                lock (_lock)
                {
                    _file = file ?? new InstalledGamesFile();
                    if (_file.Games == null)
                    {
                        _file.Games = new List<InstalledGameRecord>();
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] Failed to load installed games: {ex.Message}");
                lock (_lock)
                {
                    _file = new InstalledGamesFile();
                }
            }
        }
    }
}
