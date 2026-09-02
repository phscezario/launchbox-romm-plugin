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
    /// <summary>
    /// Provides helper methods for working with RomM game data in LaunchBox.
    /// </summary>
    public static class RommGameHelpers
    {
        /// <summary>
        /// Attempts to extract the RomM game ID from a LaunchBox game's custom fields.
        /// </summary>
        /// <param name="game">The LaunchBox game instance.</param>
        /// <param name="rommId">When this method returns, contains the parsed RomM game ID if found; otherwise, 0.</param>
        /// <returns><c>true</c> if the RomM ID was found and parsed successfully; otherwise, <c>false</c>.</returns>
        public static bool TryGetRommId(IGame game, out int rommId)
        {
            rommId = 0;

            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out rommId);
        }

        /// <summary>
        /// Gets the RomM game ID from a LaunchBox game's custom fields, returning 0 if not found.
        /// </summary>
        /// <param name="game">The LaunchBox game instance.</param>
        /// <returns>The RomM game ID, or 0 if not found or invalid.</returns>
        public static int GetRommId(IGame game)
        {
            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out var id) ? id : 0;
        }

        /// <summary>
        /// Sets a custom field value on a LaunchBox game, creating it if it doesn't exist.
        /// </summary>
        /// <param name="game">The LaunchBox game instance.</param>
        /// <param name="name">The name of the custom field.</param>
        /// <param name="value">The value to assign to the custom field.</param>
        /// <param name="overwrite">If <c>true</c>, overwrites an existing field value; if <c>false</c>, leaves existing values unchanged.</param>
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

        /// <summary>
        /// Gets the value of a custom field from a LaunchBox game.
        /// </summary>
        /// <param name="game">The LaunchBox game instance.</param>
        /// <param name="name">The name of the custom field to retrieve.</param>
        /// <returns>The field value, or <c>null</c> if the game is <c>null</c> or the field does not exist.</returns>
        public static string GetCustomField(IGame game, string name)
        {
            if (game == null) return null;
            return game.GetAllCustomFields().FirstOrDefault(f => f.Name == name)?.Value;
        }

        /// <summary>
        /// Normalizes a game title by stripping known file extensions and trimming whitespace.
        /// </summary>
        /// <param name="name">The game title to normalize.</param>
        /// <returns>The normalized title with known extensions removed, or the original value if null/whitespace.</returns>
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

        /// <summary>
        /// Parses a LaunchBox platform category into a RomM-compatible category name.
        /// </summary>
        /// <param name="category">The LaunchBox platform category string.</param>
        /// <returns>A RomM category name such as "Arcade", "Consoles", "Computers", "Handhelds", or "Others".</returns>
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

        /// <summary>
        /// Removes invalid file name characters from a string to make it safe for use as a folder name.
        /// </summary>
        /// <param name="name">The folder name to sanitize.</param>
        /// <returns>The sanitized folder name with invalid characters removed and trimmed.</returns>
        public static string SanitizeFolderName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return sanitized.Trim();
        }

        /// <summary>
        /// Ensures the directory for the specified file path exists, creating it if necessary.
        /// </summary>
        /// <param name="path">A file path whose parent directory will be created if missing.</param>
        public static void EnsureDirectoryExists(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Saves sync hash values to a game's custom fields for tracking synchronization state.
        /// </summary>
        /// <param name="game">The LaunchBox game instance.</param>
        /// <param name="remoteHash">The computed remote metadata hash to store.</param>
        public static void SaveSyncHashes(IGame game, string remoteHash)
        {
            SetCustomField(game, GameCustomFields.LastSyncedAt, DateTime.UtcNow.ToString("o"));
            SetCustomField(game, GameCustomFields.LocalMetadataHash, RommMetadataComparer.ComputeLocalMetadataHash(game));
            SetCustomField(game, GameCustomFields.RemoteMetadataHash, remoteHash);
        }

        /// <summary>
        /// Removes additional applications from a game that were previously created by the plugin
        /// and are referenced in the LaunchBox folder configuration file.
        /// </summary>
        /// <param name="game">The LaunchBox game instance.</param>
        /// <param name="installedPath">The installed game folder path.</param>
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
                if (!string.IsNullOrEmpty(app.ApplicationPath) && jsonPaths.Contains(app.ApplicationPath)
                    && app.Name?.StartsWith(RommConstants.PlatformPrefix) == true)
                {
                    game.TryRemoveAdditionalApplication(app);
                }
            }
        }

        /// <summary>
        /// Resolves a relative path against a base folder, or returns it as-is if relative to LaunchBox root.
        /// </summary>
        /// <param name="baseFolder">The base folder to resolve relative paths against.</param>
        /// <param name="path">The path to resolve.</param>
        /// <param name="fromLaunchBoxRoot">If <c>true</c>, the path is treated as relative to the LaunchBox root and returned unchanged.</param>
        /// <returns>The fully resolved absolute path.</returns>
        public static string ResolvePath(string baseFolder, string path, bool fromLaunchBoxRoot)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            if (fromLaunchBoxRoot) return path;
            return Path.GetFullPath(Path.Combine(baseFolder, path));
        }
    }
}
