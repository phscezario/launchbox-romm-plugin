using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    public class InstalledGamesService
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private InstalledGamesFile _file;

        public InstalledGamesService(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        public IReadOnlyList<InstalledGameRecord> GetAll()
        {
            lock (_lock)
            {
                return _file.Games.ToList();
            }
        }

        public InstalledGameRecord GetByGameId(int rommGameId)
        {
            lock (_lock)
            {
                return _file.Games.FirstOrDefault(g => g.RommGameId == rommGameId);
            }
        }

        public bool IsInstalled(int rommGameId)
        {
            lock (_lock)
            {
                return _file.Games.Any(g => g.RommGameId == rommGameId && !g.UninstalledAt.HasValue);
            }
        }

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

        public void RemoveUninstalled()
        {
            lock (_lock)
            {
                _file.Games.RemoveAll(g => g.UninstalledAt.HasValue);
            }

            Save();
        }

        public void Save()
        {
            try
            {
                RommLogger.Log($"[DIAG] InstalledGamesService.Save: path={_filePath}");
                string json;
                lock (_lock)
                {
                    json = JsonConvert.SerializeObject(_file, Formatting.Indented);
                }

                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var tempPath = Path.Combine(Path.GetDirectoryName(_filePath), $"installed-games.{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(tempPath, json);
                    File.Copy(tempPath, _filePath, true);
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"[RommPlugin] Failed to save installed games: {ex.Message}");
            }
        }

        public void Load()
        {
            try
            {
                RommLogger.Log($"[DIAG] InstalledGamesService.Load: path={_filePath}, exists={File.Exists(_filePath)}");
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
