using System.IO;
using Newtonsoft.Json;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    public class RommPluginSettingsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var settings = new RommPluginSettings();
            Assert.False(settings.KeepLocalData);
            Assert.False(settings.SaveLogs);
            Assert.True(settings.ProcessPendingOnStartup);
            Assert.Equal("en", settings.Language);
            Assert.False(settings.ForceFullResync);
            Assert.False(settings.ForcePushToServer);
            Assert.Null(settings.LastAutoSyncAt);
        }

        [Fact]
        public void Serialize_JsonContainsAllFields()
        {
            var settings = new RommPluginSettings
            {
                RommBaseUrl = "http://localhost:9000",
                Username = "test-username",
                Password = "test-password",
                ClientApiToken = "test-token-value",
                RomsPath = "/roms",
                KeepLocalData = true,
                SaveLogs = true,
                ProcessPendingOnStartup = false,
                Language = "pt-BR",
                ForceFullResync = true,
                ForcePushToServer = true,
            };

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<RommPluginSettings>(json);

            Assert.Equal("http://localhost:9000", deserialized.RommBaseUrl);
            Assert.Equal("test-username", deserialized.Username);
            Assert.Equal("test-password", deserialized.Password);
            Assert.Equal("test-token-value", deserialized.ClientApiToken);
            Assert.Equal("/roms", deserialized.RomsPath);
            Assert.True(deserialized.KeepLocalData);
            Assert.True(deserialized.SaveLogs);
            Assert.False(deserialized.ProcessPendingOnStartup);
            Assert.Equal("pt-BR", deserialized.Language);
            Assert.True(deserialized.ForceFullResync);
            Assert.True(deserialized.ForcePushToServer);
        }

        [Fact]
        public void Deserialize_HandlesMissingFields()
        {
            var json = "{}";
            var settings = JsonConvert.DeserializeObject<RommPluginSettings>(json);
            Assert.NotNull(settings);
            Assert.Null(settings.RommBaseUrl);
            Assert.False(settings.KeepLocalData);
            Assert.Equal("en", settings.Language);
        }
    }

    public class RommSyncInformationTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var syncInfo = new RommSyncInformation();
            Assert.False(syncInfo.SyncInProgress);
            Assert.NotNull(syncInfo.CompletedPlatformIds);
            Assert.Empty(syncInfo.CompletedPlatformIds);
            Assert.NotNull(syncInfo.CompletedGameIdsByPlatform);
            Assert.Empty(syncInfo.CompletedGameIdsByPlatform);
            Assert.NotNull(syncInfo.UnselectedPlatformIds);
            Assert.Empty(syncInfo.UnselectedPlatformIds);
        }

        [Fact]
        public void Serialize_JsonContainsAllFields()
        {
            var syncInfo = new RommSyncInformation
            {
                SyncInProgress = true,
                CompletedPlatformIds = new System.Collections.Generic.List<int> { 1, 2, 3 },
                CompletedGameIdsByPlatform = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>
                {
                    { 10, new System.Collections.Generic.List<int> { 100, 200 } }
                },
                UnselectedPlatformIds = new System.Collections.Generic.List<int> { 4, 5 },
            };

            var json = JsonConvert.SerializeObject(syncInfo);
            var deserialized = JsonConvert.DeserializeObject<RommSyncInformation>(json);

            Assert.True(deserialized.SyncInProgress);
            Assert.Equal(3, deserialized.CompletedPlatformIds.Count);
            Assert.Single(deserialized.CompletedGameIdsByPlatform);
            Assert.Equal(2, deserialized.CompletedGameIdsByPlatform[10].Count);
            Assert.Equal(2, deserialized.UnselectedPlatformIds.Count);
        }

        [Fact]
        public void Deserialize_HandlesMissingFields()
        {
            var json = "{}";
            var syncInfo = JsonConvert.DeserializeObject<RommSyncInformation>(json);
            Assert.NotNull(syncInfo);
            Assert.False(syncInfo.SyncInProgress);
            Assert.NotNull(syncInfo.CompletedPlatformIds);
            Assert.Empty(syncInfo.CompletedPlatformIds);
            Assert.NotNull(syncInfo.CompletedGameIdsByPlatform);
            Assert.Empty(syncInfo.CompletedGameIdsByPlatform);
            Assert.NotNull(syncInfo.UnselectedPlatformIds);
            Assert.Empty(syncInfo.UnselectedPlatformIds);
        }
    }
}
