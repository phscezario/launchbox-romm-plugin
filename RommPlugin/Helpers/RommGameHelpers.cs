using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Helpers
{
    public static class RommGameHelpers
    {
        public static bool TryGetRommId(IGame game, out int rommId)
        {
            rommId = 0;

            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out rommId);
        }

        public static int GetRommId(IGame game)
        {
            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out var id) ? id : 0;
        }

        public static void SetCustomField(IGame game, string name, string value, bool overwrite = true)
        {
            var field = game.GetAllCustomFields().FirstOrDefault(f => f.Name == name);

            if (field == null)
            {
                field = game.AddNewCustomField();
                field.Name = name;
                field.Value = value;

                return;
            }

            if (!overwrite)
            {
                return;
            }

            field.Value = value;
        }

        public static string GetCustomField(IGame game, string name)
        {
            if (game == null) return null;
            return game.GetAllCustomFields().FirstOrDefault(f => f.Name == name)?.Value;
        }

        public static string NormalizeGameTitle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var cleaned = name;

            while (true)
            {
                var ext = Path.GetExtension(cleaned);
                if (string.IsNullOrEmpty(ext) || !KnownExtensions.Extensions.Contains(ext))
                {
                    break;
                }

                cleaned = Path.GetFileNameWithoutExtension(cleaned);
            }

            return cleaned.Trim();
        }

        public static string ParseCategory(string category)
        {
            switch (category)
            {
                case "Arcade":
                    return "Arcade";
                case "Console":
                    return "Consoles";
                case "Operating System":
                    return "Computers";
                case "Portable Console":
                    return "Handhelds";
                default:
                    return "Others";
            }
        }

        public static string SanitizeFolderName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return sanitized.Trim();
        }

        public static void EnsureDirectoryExists(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void SaveSyncHashes(IGame game, string remoteHash)
        {
            SetCustomField(game, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
            SetCustomField(game, GameCustomFields.LocalMetadataHash, RommMetadataComparer.ComputeLocalMetadataHash(game));
            SetCustomField(game, GameCustomFields.RemoteMetadataHash, remoteHash);
        }

        public static void ClearGameAdditionalApplications(IGame game, string installedPath)
        {
            if (string.IsNullOrEmpty(installedPath)) return;

            var jsonPath = Path.Combine(installedPath, RommConstants.LaunchboxConfigFile);
            if (!File.Exists(jsonPath)) return;

            var config = JsonConvert.DeserializeObject<LaunchBoxFolderGameConfig>(File.ReadAllText(jsonPath));
            if (config == null) return;

            var jsonPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (config.AdditionalApplications != null)
                foreach (var app in config.AdditionalApplications)
                    if (!string.IsNullOrEmpty(app.Path))
                        jsonPaths.Add(ResolvePath(installedPath, app.Path, false));

            if (config.PreLoaders != null)
                foreach (var loader in config.PreLoaders)
                    if (!string.IsNullOrEmpty(loader.Path))
                        jsonPaths.Add(ResolvePath(installedPath, loader.Path, loader.FromLaunchBoxRoot ?? false));

            if (config.PosLoaders != null)
                foreach (var loader in config.PosLoaders)
                    if (!string.IsNullOrEmpty(loader.Path))
                        jsonPaths.Add(ResolvePath(installedPath, loader.Path, loader.FromLaunchBoxRoot ?? false));

            var apps = game.GetAllAdditionalApplications().ToList();
            foreach (var app in apps)
            {
                if (!string.IsNullOrEmpty(app.ApplicationPath) && jsonPaths.Contains(app.ApplicationPath))
                {
                    game.TryRemoveAdditionalApplication(app);
                }
            }
        }

        public static string ResolvePath(string baseFolder, string path, bool fromLaunchBoxRoot)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            if (fromLaunchBoxRoot) return path;
            return Path.GetFullPath(Path.Combine(baseFolder, path));
        }
    }
}
