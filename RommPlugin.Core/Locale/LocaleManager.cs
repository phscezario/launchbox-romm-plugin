using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;

namespace RommPlugin.Core.Locale
{
    /// <summary>
    /// Manages internationalization (i18n) for the plugin UI.
    /// Loads string translations from JSON locale files and provides fallback to English.
    /// </summary>
    public static class LocaleManager
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();
        private static Dictionary<string, string> _fallback = new Dictionary<string, string>();
        private static volatile bool _initialized;
        private static string _localeFolder;
        private static string _languageCode;
        private static readonly object _initLock = new object();

        /// <summary>
        /// Scans the locale folder and returns all available languages with their display names.
        /// </summary>
        /// <param name="localeFolder">Path to the folder containing locale JSON files.</param>
        /// <returns>List of language code/display name pairs, ordered alphabetically by display name.</returns>
        public static List<KeyValuePair<string, string>> GetAvailableLanguages(string localeFolder)
        {
            var result = new List<KeyValuePair<string, string>>();

            if (!Directory.Exists(localeFolder))
                return result;

            var files = Directory.GetFiles(localeFolder, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<LocaleFile>(json);

                    if (data != null && !string.IsNullOrEmpty(data.Language))
                    {
                        var code = Path.GetFileNameWithoutExtension(file);
                        result.Add(new KeyValuePair<string, string>(code, data.Language));
                    }
                }
                catch
                {
                }
            }

            return result.OrderBy(kvp => kvp.Value).ToList();
        }

        /// <summary>
        /// Initializes the locale system with the specified language.
        /// Loads the selected language file and falls back to English for missing keys.
        /// </summary>
        /// <param name="localeFolder">Path to the folder containing locale JSON files.</param>
        /// <param name="languageCode">The language code to load (e.g., "en", "pt-BR").</param>
        public static void Initialize(string localeFolder, string languageCode)
        {
            lock (_initLock)
            {
                _localeFolder = localeFolder;
                _languageCode = languageCode;
                _fallback = LoadLanguage(Path.Combine(localeFolder, "en.json"));

                if (!string.IsNullOrEmpty(languageCode) &&
                    !languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
                {
                    var langFile = Path.Combine(localeFolder, languageCode + ".json");
                    _strings = LoadLanguage(langFile);
                }
                else
                {
                    _strings = new Dictionary<string, string>(_fallback);
                }

                _initialized = true;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                if (string.IsNullOrEmpty(_localeFolder))
                {
                    _localeFolder = RommPaths.LocalesFolder;
                }

                try
                {
                    var settings = RommPluginStorage.Load();
                    Initialize(_localeFolder, settings.Language ?? "en");
                }
                catch
                {
                    _fallback = LoadLanguage(Path.Combine(_localeFolder, "en.json"));
                    _strings = new Dictionary<string, string>(_fallback);
                    _initialized = true;
                }
            }
        }

        /// <summary>
        /// Gets the translated string for the specified key.
        /// Falls back to English if the key is not found in the current language.
        /// Returns "[key]" if the key is not found in any language.
        /// </summary>
        /// <param name="key">The locale key to look up.</param>
        /// <returns>The translated string, or the key wrapped in brackets if not found.</returns>
        public static string Get(string key)
        {
            EnsureInitialized();

            if (_strings.TryGetValue(key, out var value))
                return value;

            if (_fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return "[" + key + "]";
        }

        /// <summary>
        /// Gets the translated string for the specified key and formats it with the provided arguments.
        /// Uses <see cref="string.Format(string, object[])"/> for placeholder substitution.
        /// </summary>
        /// <param name="key">The locale key to look up.</param>
        /// <param name="args">Arguments to substitute into the translated string.</param>
        /// <returns>The formatted translated string.</returns>
        public static string Get(string key, params object[] args)
        {
            var template = Get(key);

            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        private static Dictionary<string, string> LoadLanguage(string filePath)
        {
            var result = new Dictionary<string, string>();

            if (!File.Exists(filePath))
                return result;

            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<LocaleFile>(json);

                if (data?.Strings != null)
                {
                    foreach (var kvp in data.Strings)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private class LocaleFile
        {
            public string Language { get; set; }
            public Dictionary<string, string> Strings { get; set; }
        }
    }
}
