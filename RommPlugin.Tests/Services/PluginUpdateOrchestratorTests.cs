using System;
using System.Threading.Tasks;
using Moq;
using RommPlugin.Core.Interfaces;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class PluginUpdateOrchestratorTests
    {
        private static GitHubReleaseAsset ZipAsset()
        {
            return new GitHubReleaseAsset { Name = "plugin.zip", BrowserDownloadUrl = "http://example.com/plugin.zip" };
        }

        private static UpdateCheckResult AvailableResult(bool withZip = true)
        {
            return new UpdateCheckResult
            {
                UpdateAvailable = true,
                CurrentVersion = new Version(1, 0, 0),
                LatestVersion = new Version(1, 1, 0),
                ReleaseNotes = "notes",
                ZipAsset = withZip ? ZipAsset() : null
            };
        }

        private static UpdateCheckResult NoUpdateResult()
        {
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = new Version(1, 1, 0),
                LatestVersion = new Version(1, 1, 0)
            };
        }

        private static Mock<IUpdatePrompts> CreatePrompts(bool confirmResult = true, bool downloadResult = true)
        {
            var prompts = new Mock<IUpdatePrompts>();
            prompts.Setup(p => p.ConfirmUpdateNow(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(confirmResult);
            prompts.Setup(p => p.DownloadWithProgressAsync(
                    It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()))
                .ReturnsAsync(downloadResult);
            return prompts;
        }

        [Fact]
        public void HandlePending_NoPending_ReturnsFalseWithoutPrompt()
        {
            var prompts = CreatePrompts();
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                hasPendingUpdate: () => false);

            Assert.False(orchestrator.HandlePendingOnStartup());
            prompts.Verify(p => p.ConfirmUpdateNow(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void HandlePending_ConfirmYes_AppliesUpdate()
        {
            var prompts = CreatePrompts(confirmResult: true);
            var applied = false;
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                hasPendingUpdate: () => true,
                getPendingVersion: () => "1.1.0",
                applyPendingUpdate: () => { applied = true; return true; });

            Assert.True(orchestrator.HandlePendingOnStartup());
            Assert.True(applied);
        }

        [Fact]
        public void HandlePending_ConfirmNo_SkipsApply()
        {
            var prompts = CreatePrompts(confirmResult: false);
            var applied = false;
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                hasPendingUpdate: () => true,
                getPendingVersion: () => "1.1.0",
                applyPendingUpdate: () => { applied = true; return true; });

            Assert.True(orchestrator.HandlePendingOnStartup());
            Assert.False(applied);
        }

        [Fact]
        public void HandlePending_ApplyFails_ShowsInfo()
        {
            var prompts = CreatePrompts(confirmResult: true);
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                hasPendingUpdate: () => true,
                getPendingVersion: () => "1.1.0",
                applyPendingUpdate: () => false);

            Assert.True(orchestrator.HandlePendingOnStartup());
            prompts.Verify(p => p.ShowInfo(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Startup_NoUpdateAvailable_NoPrompts()
        {
            var prompts = CreatePrompts();
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(NoUpdateResult()));

            await orchestrator.CheckAndPromptOnStartupAsync();

            prompts.Verify(p => p.ConfirmUpdateNow(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            prompts.Verify(p => p.DownloadWithProgressAsync(
                It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Startup_UpdateAvailable_ConfirmNo_SkipsDownload()
        {
            var prompts = CreatePrompts(confirmResult: false);
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(AvailableResult()));

            await orchestrator.CheckAndPromptOnStartupAsync();

            prompts.Verify(p => p.DownloadWithProgressAsync(
                It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Startup_UpdateAvailable_DownloadFails_ShowsInfoWithoutApply()
        {
            var prompts = CreatePrompts(confirmResult: true, downloadResult: false);
            var applied = false;
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(AvailableResult()),
                applyPendingUpdate: () => { applied = true; return true; });

            await orchestrator.CheckAndPromptOnStartupAsync();

            Assert.False(applied);
            prompts.Verify(p => p.ShowInfo(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Startup_FullYesFlow_AppliesUpdate()
        {
            var prompts = CreatePrompts(confirmResult: true, downloadResult: true);
            var applied = false;
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(AvailableResult()),
                applyPendingUpdate: () => { applied = true; return true; });

            await orchestrator.CheckAndPromptOnStartupAsync();

            Assert.True(applied);
            prompts.Verify(p => p.ConfirmUpdateNow(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Startup_ApplyDeclinedAfterDownload_SkipsApply()
        {
            var prompts = new Mock<IUpdatePrompts>();
            prompts.SetupSequence(p => p.ConfirmUpdateNow(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true)
                .Returns(false);
            prompts.Setup(p => p.DownloadWithProgressAsync(
                    It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var applied = false;
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(AvailableResult()),
                applyPendingUpdate: () => { applied = true; return true; });

            await orchestrator.CheckAndPromptOnStartupAsync();

            Assert.False(applied);
        }

        [Fact]
        public async Task Startup_NoZipAsset_StaysSilent()
        {
            var prompts = CreatePrompts();
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(AvailableResult(withZip: false)));

            await orchestrator.CheckAndPromptOnStartupAsync();

            prompts.Verify(p => p.ConfirmUpdateNow(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            prompts.Verify(p => p.ShowInfo(It.IsAny<string>()), Times.Never);
            prompts.Verify(p => p.DownloadWithProgressAsync(
                It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Manual_NoUpdate_ShowsCurrentVersionInfo()
        {
            var prompts = CreatePrompts();
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(NoUpdateResult()));

            await orchestrator.RunManualCheckAsync();

            prompts.Verify(p => p.ShowInfo(It.IsAny<string>()), Times.Once);
            prompts.Verify(p => p.DownloadWithProgressAsync(
                It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Manual_NoZipAsset_ShowsNoAssetInfo()
        {
            var prompts = CreatePrompts();
            var orchestrator = new PluginUpdateOrchestrator(
                prompts.Object,
                checkForUpdateAsync: () => Task.FromResult(AvailableResult(withZip: false)));

            await orchestrator.RunManualCheckAsync();

            prompts.Verify(p => p.ShowInfo(It.IsAny<string>()), Times.Once);
            prompts.Verify(p => p.DownloadWithProgressAsync(
                It.IsAny<GitHubReleaseAsset>(), It.IsAny<string>()), Times.Never);
        }
    }
}
