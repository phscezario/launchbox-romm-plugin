using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RommPlugin.ApiClient;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Helpers;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Synchronizes screenshots and cover art between LaunchBox games and the RomM server.
    /// Handles bidirectional screenshot sync, cover art download, and image management.
    /// </summary>
    public class RommScreenshotSync : IRommScreenshotSync
    {
        private readonly IRommApiClient _api;

        /// <summary>
        /// Initializes a new instance of the <see cref="RommScreenshotSync"/> class.
        /// </summary>
        /// <param name="api">The RomM API client used for downloading and uploading screenshots.</param>
        public RommScreenshotSync(IRommApiClient api)
        {
            _api = api;
        }

        /// <inheritdoc/>
        public async Task SyncScreenshotsBidirectional(IGame game, RommGame remoteGame, RommPluginSettings settings)
        {
            try
            {
                if (remoteGame == null) return;

                var remoteScreenshots = remoteGame.UserScreenshots ?? new List<RommScreenshot>();
                var localImages = game.GetAllImagesWithDetails()
                    .Where(i => i.ImageType == RommConstants.ImageTypeScreenshot)
                    .ToList();

                var localFileNames = new HashSet<string>(
                    localImages.Select(i => Path.GetFileNameWithoutExtension(i.FilePath)),
                    StringComparer.OrdinalIgnoreCase);

                var remoteFileNames = new HashSet<string>(
                    remoteScreenshots.Select(s => s.FileNameNoExt ?? Path.GetFileNameWithoutExtension(s.FileName ?? "")),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var localImage in localImages)
                {
                    var localName = Path.GetFileNameWithoutExtension(localImage.FilePath);
                    if (!remoteFileNames.Contains(localName) && File.Exists(localImage.FilePath))
                    {
                        try
                        {
                            var screenshotId = await _api.UploadScreenshotAsync(remoteGame.Id, localImage.FilePath);
                            if (screenshotId > 0 && settings.IsAdmin && settings.PublicScreenshots)
                            {
                                await _api.SetScreenshotPublicAsync(screenshotId);
                            }
                            RommLogger.Log($"Screenshot uploaded for game {remoteGame.Id}: {localName}");
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to upload screenshot {localName} for game {remoteGame.Id}: {ex.Message}");
                        }
                    }
                }

                foreach (var remoteScreenshot in remoteScreenshots)
                {
                    var remoteName = remoteScreenshot.FileNameNoExt
                        ?? Path.GetFileNameWithoutExtension(remoteScreenshot.FileName ?? "");

                    if (!string.IsNullOrEmpty(remoteName) && !localFileNames.Contains(remoteName))
                    {
                        try
                        {
                            var safeFileName = Path.GetFileName(remoteScreenshot.FileName ?? $"{remoteScreenshot.Id}.jpg");
                            var tempPath = Path.Combine(Path.GetTempPath(), safeFileName);
                            try
                            {
                                await _api.DownloadScreenshotAsync(remoteScreenshot.Id, tempPath);

                                if (File.Exists(tempPath))
                                {
                                    var imagePath = game.GetNextAvailableImageFilePath(".jpg", RommConstants.ImageTypeScreenshot, null);
                                    RommGameHelpers.EnsureDirectoryExists(imagePath);
                                    File.Copy(tempPath, imagePath, true);
                                    File.Delete(tempPath);
                                    RommLogger.Log($"Screenshot downloaded for game {remoteGame.Id}: {remoteName}");
                                }
                            }
                            catch
                            {
                                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                                throw;
                            }
                        }
                        catch (Exception ex)
                        {
                            RommLogger.LogError($"Failed to download screenshot {remoteName} for game {remoteGame.Id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Error syncing screenshots for game {remoteGame?.Id}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task DownloadAndSetCoverArt(IGame game, RommGame rommGame)
        {
            var coverUrl = !string.IsNullOrEmpty(rommGame.PathCoverSmall)
                ? rommGame.PathCoverSmall
                : rommGame.UrlCover;

            if (!string.IsNullOrEmpty(coverUrl))
            {
                try
                {
                    var coverBytes = await _api.DownloadBytesAsync(coverUrl);

                    if (coverBytes == null || coverBytes.Length == 0)
                    {
                        RommLogger.Log($"Cover art download returned empty for {game.Title}");
                        return;
                    }

                    var imagePath = game.GetNextAvailableImageFilePath(".jpg", RommConstants.ImageTypeBoxFront, null);
                    RommLogger.Log($"Cover art image path: {imagePath}");
                    RommGameHelpers.EnsureDirectoryExists(imagePath);

                    var tempPath = Path.GetTempFileName();
                    try
                    {
                        File.WriteAllBytes(tempPath, coverBytes);
                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                        }
                        File.Move(tempPath, imagePath);
                    }
                    catch
                    {
                        try { File.Delete(tempPath); } catch { }
                        throw;
                    }

                    RommLogger.Log($"Cover art downloaded for {game.Title}: {imagePath}");
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"Failed to download cover for {game.Title}: {ex.Message}");
                }
            }
        }

        /// <inheritdoc/>
        public string GetCoverImagePath(IGame game)
        {
            var images = game.GetAllImagesWithDetails();

            foreach (var image in images)
            {
                if (image.ImageType == RommConstants.ImageTypeBoxFront)
                {
                    return image.FilePath;
                }

                if (image.ImageType == RommConstants.ImageTypeFanartBoxFront)
                {
                    return image.FilePath;
                }

                if (image.ImageType == RommConstants.ImageTypeAdvertisementFlyerFront)
                {
                    return image.FilePath;
                }
            }

            return "";
        }

        /// <inheritdoc/>
        public bool HasAnyBoxFrontImage(IGame game)
        {
            var images = game.GetAllImagesWithDetails();

            foreach (var image in images)
            {
                if (image.ImageType == RommConstants.ImageTypeBoxFront)
                {
                    return true;
                }

                if (image.ImageType == RommConstants.ImageTypeFanartBoxFront)
                {
                    return true;
                }

                if (image.ImageType == RommConstants.ImageTypeAdvertisementFlyerFront)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void DeleteGameImages(IGame game)
        {
            var imagesFolder = RommHelpers.GetLaunchBoxImagesFolder();
            var platformFolder = game.Platform ?? "Unknown";
            var title = game.Title ?? "Unknown";

            var gameImagesDir = Path.Combine(imagesFolder, RommGameHelpers.SanitizeFolderName(platformFolder), RommGameHelpers.SanitizeFolderName(title));

            if (Directory.Exists(gameImagesDir))
            {
                Directory.Delete(gameImagesDir, true);
                RommLogger.Log($"Deleted images for removed game: {title}");
            }
        }
    }
}
