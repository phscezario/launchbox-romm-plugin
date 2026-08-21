using System;
using System.IO;
using RommPlugin.Core.Locale;
using Xunit;

namespace RommPlugin.Tests.Services
{
    [Collection("Locale")]
    public class LocaleManagerTests : IDisposable
    {
        private readonly string _tempDir;

        public LocaleManagerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "RommTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var enJson = @"{
  ""Language"": ""English"",
  ""Strings"": {
    ""test.key"": ""Hello World"",
    ""test.format"": ""Value is {0}"",
    ""test.fallback_only"": ""From English""
  }
}";
            File.WriteAllText(Path.Combine(_tempDir, "en.json"), enJson);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }

            var localesPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "RommPlugin.Core", "Locales");

            if (Directory.Exists(localesPath))
            {
                LocaleManager.Initialize(localesPath, "en");
            }
        }

        [Fact]
        public void Get_ReturnsValue_WhenKeyExists()
        {
            LocaleManager.Initialize(_tempDir, "en");
            var result = LocaleManager.Get("test.key");
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void Get_ReturnsFormattedValue_WithArgs()
        {
            LocaleManager.Initialize(_tempDir, "en");
            var result = LocaleManager.Get("test.format", "42");
            Assert.Equal("Value is 42", result);
        }

        [Fact]
        public void Get_ReturnsBrackets_WhenKeyNotFound()
        {
            LocaleManager.Initialize(_tempDir, "en");
            var result = LocaleManager.Get("nonexistent.key");
            Assert.Equal("[nonexistent.key]", result);
        }

        [Fact]
        public void Initialize_FallsBackToEnglish_WhenLanguageNotFound()
        {
            var ptJson = @"{
  ""Language"": ""Portuguese"",
  ""Strings"": {
    ""test.key"": ""Olá Mundo""
  }
}";
            File.WriteAllText(Path.Combine(_tempDir, "pt.json"), ptJson);

            LocaleManager.Initialize(_tempDir, "pt");
            var result = LocaleManager.Get("test.fallback_only");
            Assert.Equal("From English", result);
        }

        [Fact]
        public void Initialize_UsesSelectedLanguage_WhenAvailable()
        {
            var ptJson = @"{
  ""Language"": ""Portuguese"",
  ""Strings"": {
    ""test.key"": ""Olá Mundo""
  }
}";
            File.WriteAllText(Path.Combine(_tempDir, "pt.json"), ptJson);

            LocaleManager.Initialize(_tempDir, "pt");
            var result = LocaleManager.Get("test.key");
            Assert.Equal("Olá Mundo", result);
        }

        [Fact]
        public void GetAvailableLanguages_ReturnsLanguages()
        {
            var result = LocaleManager.GetAvailableLanguages(_tempDir);
            Assert.Single(result);
            Assert.Equal("en", result[0].Key);
            Assert.Equal("English", result[0].Value);
        }

        [Fact]
        public void GetAvailableLanguages_ReturnsEmpty_WhenFolderNotFound()
        {
            var result = LocaleManager.GetAvailableLanguages(Path.Combine(_tempDir, "nonexistent"));
            Assert.Empty(result);
        }
    }
}
