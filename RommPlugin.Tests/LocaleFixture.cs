using System;
using System.IO;
using System.Reflection;
using RommPlugin.Core.Locale;

namespace RommPlugin.Tests
{
    public class LocaleFixture : IDisposable
    {
        public LocaleFixture()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            var localesPath = FindLocalesPath();
            if (localesPath != null)
            {
                LocaleManager.Initialize(localesPath, "en");
            }
        }

        private static string FindLocalesPath()
        {
            var assemblyDir = Path.GetDirectoryName(typeof(LocaleFixture).Assembly.Location);

            var candidates = new[]
            {
                Path.Combine(assemblyDir, "Locales"),
                Path.Combine(assemblyDir, "..", "..", "RommPlugin.Core", "Locales"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Locales"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "RommPlugin.Core", "Locales"),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "en.json")))
                {
                    return fullPath;
                }
            }

            return null;
        }

        public void Dispose() { }
    }
}
