using System;
using System.IO;
using RommPlugin.Core.Locale;

namespace RommPlugin.Tests
{
    public class LocaleFixture : IDisposable
    {
        public LocaleFixture()
        {
            var localesPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "RommPlugin.Core", "Locales");

            if (Directory.Exists(localesPath))
            {
                LocaleManager.Initialize(localesPath, "en");
            }
        }

        public void Dispose() { }
    }
}
