using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RommPlugin.ApiClient;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Forms;
using RommPlugin.UI.Helpers;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public class RommSyncService
    {
        private RommApiClient _api;

        public void SetApi(RommApiClient api)
        {
            _api = api;
        }

        public async Task SyncAsync()
        {
            await ProgressRunner.RunAsync(
                "Starting sync from RomM...",
                async progress =>
                {
                    var settings = RommPluginStorage.Load();

                    _api.SetBasicAuthentication(settings.Username, settings.Password);

                    var dataManager = PluginHelper.DataManager;

                    var platforms = dataManager.GetAllPlatforms()
                        .Where(p => p.Name != null && p.Name.StartsWith("RomM | "))
                        .ToList();

                    var rommGamesOnly = dataManager.GetAllGames()
                        .Where(g => g.Platform != null && g.Platform.StartsWith("RomM | "))
                        .ToList();

                    var platformCategories = dataManager.GetAllPlatformCategories()
                        .Where(c => c.Name != null && c.Name.StartsWith("RomM | "))
                        .ToList();

                    var rommPlatforms = await _api.GetPlatformsAsync();

                    if (rommPlatforms == null || rommPlatforms.Count == 0)
                    {
                        return;
                    }

                    RommLogger.Log($"Sync started: {rommPlatforms.Count} platforms found, {rommGamesOnly.Count} local RomM games");

                    var localGamesById = new Dictionary<int, IGame>();

                    foreach (var game in rommGamesOnly)
                    {
                        if (TryGetRommId(game, out var id))
                        {
                            localGamesById[id] = game;
                        }
                    }

                    var selectedPlatformIds = new HashSet<int>();

                    var list = rommPlatforms.Select(p => new PlatformSelection
                    {
                        Id = p.Id,
                        Name = string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName,
                        Selected = true
                    }).ToList();

                    using (var form = new RommPlatformSelectorForm(list))
                    {
                        if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        selectedPlatformIds = form.Platforms
                            .Where(p => p.Selected)
                            .Select(p => p.Id)
                            .ToHashSet();
                    }

                    var hasChanges = false;
                    var platformCompleted = 0;
                    var platformTotal = selectedPlatformIds.Count;

                    var newPlatforms = new List<string>();
                    var removedGames = new List<IGame>();

                    foreach (var rommPlatform in rommPlatforms)
                    {
                        var parsedCategory = parseCategory(rommPlatform.Category);
                        var rommCategoryName = $"RomM | {parsedCategory}";

                        var rommCategory = platformCategories
                            .FirstOrDefault(p => p.Name == rommCategoryName);

                        if (rommCategory == null)
                        {
                            rommCategory = dataManager.AddNewPlatformCategory(rommCategoryName);
                            platformCategories.Add(rommCategory);
                            hasChanges = true;
                        }

                        var name = !string.IsNullOrWhiteSpace(rommPlatform.CustomName)
                            ? rommPlatform.CustomName
                            : rommPlatform.Name;

                        var platformName = $"RomM | {name}";

                        var platform = platforms
                            .FirstOrDefault(p => p.Name == platformName);

                        if (platform == null)
                        {
                            platform = dataManager.AddNewPlatform(platformName);
                            platform.Category = rommCategoryName;
                            platforms.Add(platform);
                            hasChanges = true;

                            settings.CurrentPlatforms.Add(new RommCurrentPlatform
                            {
                                Id = rommPlatform.Id,
                                Name = platformName
                            });

                            newPlatforms.Add(platformName);
                        }

                        if (!selectedPlatformIds.Contains(rommPlatform.Id))
                        { 
                            continue;
                        }

                        platformCompleted++;

                        var rommGames = await _api.GetAllGamesByPlatformAsync(rommPlatform.Id);

                        if (rommGames == null)
                        {
                            continue;
                        }

                        RommLogger.Log($"Platform '{platform.Name}': {rommGames.Count} games to process");

                        progress.SetTitle($"RomM: Syncing {platform.Name}");

                        var completedGames = 0;
                        var totalGames = rommGames.Count;

                        var serverGameIds = new HashSet<int>();

                        var platformProgress = false;

                        foreach (var rommGame in rommGames)
                        {
                            if (!platformProgress)
                            {
                                progress.SetIndeterminate(false);
                                platformProgress = true;
                            }

                            progress.SetStatus($"Platform {platformCompleted}/{platformTotal} | Games {completedGames}/{totalGames}");
                            progress.SetProgress((completedGames * 100) / Math.Max(totalGames, 1));

                            serverGameIds.Add(rommGame.Id);

                            if (localGamesById.TryGetValue(rommGame.Id, out var existingGame))
                            {
                                UpdateGame(existingGame, rommGame, platform.Name, !settings.KeepLocalData);
                                RommLogger.Log($"Game {rommGame.Id} updated: {NormalizeGameTitle(rommGame.Name)}");
                                hasChanges = true;
                            }
                            else
                            {
                                var normalizedTitle = NormalizeGameTitle(rommGame.Name);
                                RommLogger.Log($"Game {rommGame.Id} created: {normalizedTitle}");
                                var game = dataManager.AddNewGame(normalizedTitle);

                                game.Platform = platform.Name;

                                var isFolderGame = rommGame.HasMultipleFiles;

                                SetCustomField(game, GameCustomFields.GameId, rommGame.Id.ToString());
                                SetCustomField(game, GameCustomFields.PlatformId, rommPlatform.Id.ToString());
                                SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
                                SetCustomField(game, GameCustomFields.FileName, (rommGame.FsName + (isFolderGame ? ".zip" : "")) ?? "");
                                SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());

                                AddInstallUninstallIfMissing(game, rommGame.Id);

                                game.Installed = game.Installed != null ? game.Installed : false;

                                localGamesById[rommGame.Id] = game;
                                hasChanges = true;
                            }

                            var currentGame = localGamesById[rommGame.Id];
                            ApplyServerMetadata(currentGame, rommGame);

                            if (!HasAnyBoxFrontImage(currentGame))
                            {
                                await DownloadAndSetCoverArt(currentGame, rommGame);
                            }

                            completedGames++;
                        }

                        var localGamesFromPlatform = dataManager.GetAllGames()
                            .Where(g =>
                                g.Platform != null &&
                                g.Platform.StartsWith("RomM | ") &&
                                g.GetAllCustomFields()
                                 .Any(f => f.Name == GameCustomFields.PlatformId && f.Value == rommPlatform.Id.ToString()))
                            .ToList();

                        foreach (var localGame in localGamesFromPlatform)
                        {
                            var rommId = GetRommId(localGame);

                            if (rommId == 0)
                            {
                                continue;
                            }

                            if (!serverGameIds.Contains(rommId))
                            {
                                dataManager.TryRemoveGame(localGame);
                                removedGames.Add(localGame);
                                RommLogger.Log($"Game {rommId} removed from platform '{platform.Name}' (not in server)");
                                hasChanges = true;
                            }
                        }

                    }

                    if (newPlatforms.Any())
                    {
                        MessageBox.Show(
                            "RomM new platforms detected:\n\n" + string.Join("\n", newPlatforms) +
                            "\n\nYou can sync them later."
                        );
                    }

                    foreach (var removedGame in removedGames)
                    {
                        DeleteGameImages(removedGame);
                    }

                    if (hasChanges)
                    {
                        dataManager.Save();
                        dataManager.ForceReload();
                    }

                    RommLogger.Log($"Sync completed. Changes saved: {hasChanges}");

                    CheckAndSavePlatforms(rommPlatforms, platforms);

                    MessageBox.Show("RomM sync completed successfully.");
                }
            );
        }

        private bool TryGetRommId(IGame game, out int rommId)
        {
            rommId = 0;

            var value = game.GetAllCustomFields().FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out rommId);
        }

        private int GetRommId(IGame game)
        {
            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out var id) ? id : 0;
        }

        public object RoomImageService { get; private set; }

        private string NormalizeGameTitle(string name)
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

        private void UpdateGame(IGame game, RommGame rommGame, string platformName, bool overwriteLocalData)
        {
            if (overwriteLocalData)
            {
                game.Title = NormalizeGameTitle(rommGame.Name);

                game.Platform = platformName;
            }

            var isFolderGame = rommGame.HasMultipleFiles;

            SetCustomField(game, GameCustomFields.RemotePath, rommGame.FsPath ?? "");
            SetCustomField(game, GameCustomFields.FileName, (rommGame.FsName + (isFolderGame ? ".zip" : "")) ?? "");
            SetCustomField(game, GameCustomFields.IsFolderGame, isFolderGame.ToString());
        }

        private void AddInstallUninstallIfMissing(IGame game, int rommId)
        {
            var hasInstallApp = game.GetAllAdditionalApplications()
                .Any(a => a.Name == "Install (RomM)");

            if (!hasInstallApp)
            {
                var installApp = game.AddNewAdditionalApplication();
                installApp.Name = "Install (RomM)";
                installApp.ApplicationPath = ".\\Plugins\\RomM LaunchBox Integration\\RommPlugin.CLI.exe";
                installApp.CommandLine = $"install {rommId}";
                installApp.AutoRunAfter = false;
            }

            var hasUninstallApp = game.GetAllAdditionalApplications()
                .Any(a => a.Name == "Uninstall (RomM)");

            if (!hasUninstallApp)
            {
                var uninstallApp = game.AddNewAdditionalApplication();
                uninstallApp.Name = "Uninstall (RomM)";
                uninstallApp.ApplicationPath = ".\\Plugins\\RomM LaunchBox Integration\\RommPlugin.CLI.exe";
                uninstallApp.CommandLine = $"uninstall {rommId}";
                uninstallApp.AutoRunAfter = false;
            }
        }

        private string parseCategory(string category)
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

        private void SetCustomField(IGame game, string name, string value, bool overwrite = true)
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

        private void CheckAndSavePlatforms(List<RommPlatform> rommPlatforms, List<IPlatform> launchboxPlatforms)
        {
            var settings = RommPluginStorage.Load();
            var manager = PluginHelper.DataManager;
            var serverIds = new HashSet<int>(rommPlatforms.Select(p => p.Id));

            foreach (var rommPlatform in rommPlatforms)
            {
                var name = !string.IsNullOrWhiteSpace(rommPlatform.CustomName)
                    ? rommPlatform.CustomName
                    : rommPlatform.Name;

                var platformName = $"RomM | {name}";

                var existing = settings.CurrentPlatforms
                    .FirstOrDefault(p => p.Id == rommPlatform.Id);

                if (existing == null)
                {
                    settings.CurrentPlatforms.Add(new RommCurrentPlatform
                    {
                        Id = rommPlatform.Id,
                        Name = platformName
                    });
                }
                else
                {
                    existing.Name = platformName;
                }
            }

            var removedPlatforms = settings.CurrentPlatforms
                .Where(p => !serverIds.Contains(p.Id))
                .ToList();

            foreach (var removed in removedPlatforms)
            {
                var platform = launchboxPlatforms
                    .FirstOrDefault(p => p.Name == removed.Name);

                if (platform != null)
                {
                    manager.TryRemovePlatform(platform);
                }

                settings.CurrentPlatforms.Remove(removed);
            }

            RommPluginStorage.Save(settings);
            manager.Save();
        }

        public async Task UpdateServerMetadata(string username, string password)
        {
            await ProgressRunner.RunAsync(
                "Reset Metadata in RomM server...",
                async progress =>
                {
                    _api.SetBasicAuthentication(username, password);

                    var dataManager = PluginHelper.DataManager;
                    var settings = RommPluginStorage.Load();

                    if (settings.CurrentPlatforms == null || settings.CurrentPlatforms.Count == 0)
                    {
                        MessageBox.Show("No RomM platforms available.");
                        return;
                    }

                    var list = settings.CurrentPlatforms
                        .Select(p => new PlatformSelection
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Selected = true
                        })
                        .ToList();

                    using (var form = new RommPlatformSelectorForm(list))
                    {
                        if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }
                    }

                    var selectedPlatforms = list
                        .Where(p => p.Selected)
                        .Select(p => p.Name)
                        .ToHashSet();

                    var rommGamesOnly = dataManager.GetAllGames()
                            .Where(g => g.Platform != null && selectedPlatforms.Contains(g.Platform))
                            .ToList();

                    if (rommGamesOnly == null || rommGamesOnly.Count == 0)
                    {
                        MessageBox.Show("No RomM games available.");
                        return;
                    }

                    RommLogger.Log($"Update metadata started: {rommGamesOnly.Count} games");

                    var completedGames = 0;
                    var gamesTotal = rommGamesOnly.Count;
                    var progressLock = new object();
                    var failedGames = new List<string>();
                    var semaphore = new SemaphoreSlim(5);

                    progress.SetTitle($"RomM: Update all metadata");

                    var tasks = rommGamesOnly.Select(async game =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            TryGetRommId(game, out int rommId);

                            var artworkPath = GetCoverImagePath(game);
                            var originalArtwork = artworkPath;

                            if (!string.IsNullOrEmpty(artworkPath) && File.Exists(artworkPath))
                            {
                                artworkPath = RommImageService.EnsureRgbJpeg(artworkPath);
                            }

                            var request = new RommUpdateGameRequest
                            {
                                Name = game.Title,
                                Summary = game.Notes,
                                LaunchboxId = game.LaunchBoxDbId,
                                RawLaunchboxMetadata = LaunchboxMetadaService.BuildLaunchboxMetadata(game),
                                ArtworkPath = artworkPath
                            };

                            try
                            {
                                await _api.UpdateGameById(rommId, request);
                                RommLogger.Log($"Game {rommId} metadata updated on server");
                            }
                            catch (Exception ex)
                            {
                                var platform = game.Platform ?? "Unknown";
                                var gameName = $"{platform}/{game.Title} (RomM ID: {rommId})";
                                RommLogger.LogError($"Update failed for {gameName}: {ex.Message}");
                                RommLogger.LogException(ex);
                                lock (progressLock) { failedGames.Add(gameName); }
                            }

                            if (!string.IsNullOrEmpty(artworkPath) && artworkPath != originalArtwork)
                            {
                                File.Delete(artworkPath);
                            }

                            var done = Interlocked.Increment(ref completedGames);
                            if (done % 10 == 0)
                            {
                                lock (progressLock)
                                {
                                    progress.SetStatus($"Games: {done} of {gamesTotal}");
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);

                    RommLogger.Log($"Update metadata completed: {gamesTotal} games");

                    if (failedGames.Count > 0)
                    {
                        RommLogger.LogError($"Update failed for {failedGames.Count} game(s). Check the log file for details.");
                        MessageBox.Show(
                            $"{failedGames.Count} game(s) failed to update.\n\nCheck the log file for details.",
                            "RomM Update - Errors",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                    else
                    {
                        MessageBox.Show("All metadata on server has been updated with local metadata");
                    }
                }
            );
        }

        private string GetCoverImagePath(IGame game)
        {
            var images = game.GetAllImagesWithDetails();

            foreach (var image in images)
            {
                if (image.ImageType == "Box - Front")
                {
                    return image.FilePath;
                }

                if (image.ImageType == "Fanart - Box - Front")
                {
                    return image.FilePath;
                }

                if (image.ImageType == "Advertisement Flyer - Front")
                {
                    return image.FilePath;
                }
            }

            return "";
        }

        private bool HasAnyBoxFrontImage(IGame game)
        {
            var images = game.GetAllImagesWithDetails();

            foreach (var image in images)
            {
                if (image.ImageType == "Box - Front")
                {
                    return true;
                }

                if (image.ImageType == "Fanart - Box - Front")
                {
                    return true;
                }

                if (image.ImageType == "Advertisement Flyer - Front")
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyServerMetadata(IGame game, RommGame rommGame)
        {
            var settings = RommPluginStorage.Load();
            var shouldOverwrite = !settings.KeepLocalData;

            var launchboxMeta = rommGame.LaunchBoxMetadata;
            var ssMeta = rommGame.SsMetadata;
            var igdbMeta = rommGame.IgdbMetadata;
            var meta = rommGame.Metadatum;

            ApplyReleaseDate(game, launchboxMeta, ssMeta, igdbMeta, meta, shouldOverwrite);
            ApplyMaxPlayers(game, launchboxMeta, ssMeta, shouldOverwrite);
            ApplyStringField(game.ReleaseType, v => game.ReleaseType = v,
                launchboxMeta?.ReleaseType, null, null, null, shouldOverwrite);
            ApplyPlayMode(game, launchboxMeta, shouldOverwrite);
            ApplyVideoUrl(game, launchboxMeta, igdbMeta, shouldOverwrite);
            ApplyCommunityRating(game, launchboxMeta, igdbMeta, meta, shouldOverwrite);
            ApplyIntField(() => game.CommunityStarRatingTotalVotes, v => game.CommunityStarRatingTotalVotes = v,
                launchboxMeta?.CommunityRatingCount, null, null, null, shouldOverwrite);
            ApplyStringField(game.WikipediaUrl, v => game.WikipediaUrl = v,
                launchboxMeta?.WikipediaUrl, null, null, null, shouldOverwrite);
            ApplyStringField(game.Rating, v => game.Rating = v,
                launchboxMeta?.Esrb, null, null, null, shouldOverwrite);

            if (shouldOverwrite || string.IsNullOrEmpty(game.Notes))
            {
                game.Notes = ssMeta?.Synopsis ?? ssMeta?.Description ?? rommGame.Summary ?? game.Notes;
            }

            if (rommGame.LaunchboxId != null && rommGame.LaunchboxId > 0)
            {
                game.LaunchBoxDbId = rommGame.LaunchboxId;
            }
        }

        private void ApplyReleaseDate(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, IgdbMetadata igdb, RommGameMeta meta, bool overwrite)
        {
            if (overwrite || game.ReleaseDate == null)
            {
                DateTime? date = null;

                if (lb?.FirstReleaseDate != null)
                    date = UnixToDateTime(lb.FirstReleaseDate.Value);
                else if (ss?.ReleaseDate != null && DateTime.TryParse(ss.ReleaseDate, out var ssDate))
                    date = ssDate;
                else if (igdb?.FirstReleaseDate != null)
                    date = UnixToDateTime(igdb.FirstReleaseDate.Value);
                else if (meta?.FirstReleaseDate != null)
                    date = UnixToDateTime(meta.FirstReleaseDate.Value);

                if (date != null)
                    game.ReleaseDate = date.Value;
            }
        }

        // RomM 4.x's merged metadata view returns first_release_date in milliseconds,
        // while older servers (and this plugin's own writes) use seconds. Values above the
        // threshold cannot be valid seconds, so treat them as milliseconds.
        private static DateTime UnixToDateTime(long value)
        {
            var dto = value > 100_000_000_000L
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return dto.DateTime;
        }

        private void ApplyMaxPlayers(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, bool overwrite)
        {
            if (overwrite || game.MaxPlayers == null || game.MaxPlayers == 0)
            {
                if (lb?.MaxPlayers != null)
                    game.MaxPlayers = lb.MaxPlayers.Value;
                else if (ss?.Players != null && int.TryParse(ss.Players, out var players))
                    game.MaxPlayers = players;
            }
        }

        private void ApplyPlayMode(IGame game, LaunchBoxMetadataModel lb, bool overwrite)
        {
            if (overwrite || string.IsNullOrEmpty(game.PlayMode))
            {
                if (lb?.Cooperative == true)
                    game.PlayMode = "Cooperative";
            }
        }

        private void ApplyVideoUrl(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, bool overwrite)
        {
            if (overwrite || string.IsNullOrEmpty(game.VideoUrl))
            {
                var videoId = lb?.YoutubeVideoId ?? igdb?.YoutubeVideoId;

                if (!string.IsNullOrEmpty(videoId))
                    game.VideoUrl = $"https://www.youtube.com/watch?v={videoId}";
            }
        }

        private void ApplyCommunityRating(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, RommGameMeta meta, bool overwrite)
        {
            if (overwrite || game.CommunityStarRating == 0)
            {
                if (lb?.CommunityRating > 0)
                    game.CommunityStarRating = lb.CommunityRating;
                else if (igdb?.TotalRating != null)
                    game.CommunityStarRating = (float)igdb.TotalRating.Value;
                else if (meta?.AverageRating != null)
                    game.CommunityStarRating = (float)meta.AverageRating.Value;
            }

            if (overwrite || game.CommunityStarRatingTotalVotes == 0)
            {
                if (lb?.CommunityRatingCount > 0)
                    game.CommunityStarRatingTotalVotes = lb.CommunityRatingCount;
            }
        }

        private void ApplyStringField(string currentValue, Action<string> setter,
            string lbValue, string ssValue, string igdbValue, string metaValue,
            bool shouldOverwrite)
        {
            if (shouldOverwrite || string.IsNullOrEmpty(currentValue))
            {
                var value = lbValue ?? ssValue ?? igdbValue ?? metaValue;

                if (!string.IsNullOrEmpty(value))
                    setter(value);
            }
        }

        private void ApplyIntField(Func<int> getter, Action<int> setter,
            int? lbValue, int? ssValue, int? igdbValue, int? metaValue,
            bool shouldOverwrite)
        {
            if (shouldOverwrite || getter() == 0)
            {
                var value = lbValue ?? ssValue ?? igdbValue ?? metaValue;

                if (value != null && value.Value > 0)
                    setter(value.Value);
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private string GetLaunchBoxImagesFolder()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var launchBoxRoot = Path.GetDirectoryName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(assemblyPath)
                )
            );

            return Path.Combine(launchBoxRoot, "Images");
        }

        private async Task DownloadAndSetCoverArt(IGame game, RommGame rommGame)
        {
            var coverUrl = !string.IsNullOrEmpty(rommGame.PathCoverSmall)
                ? rommGame.PathCoverSmall
                : rommGame.UrlCover;

            if (!string.IsNullOrEmpty(coverUrl))
            {
                try
                {
                    var coverBytes = await _api.DownloadBytesAsync(coverUrl);

                    var imagePath = game.GetNextAvailableImageFilePath(".jpg", "Box - Front", null);
                    RommLogger.Log($"Cover art image path: {imagePath}");
                    EnsureDirectoryExists(imagePath);
                    File.WriteAllBytes(imagePath, coverBytes);

                    RommLogger.Log($"Cover art downloaded for {game.Title}: {imagePath}");
                }
                catch (Exception ex)
                {
                    RommLogger.LogError($"Failed to download cover for {game.Title}: {ex.Message}");
                }
            }
        }

        private void DeleteGameImages(IGame game)
        {
            var imagesFolder = GetLaunchBoxImagesFolder();
            var platformFolder = game.Platform ?? "Unknown";
            var title = game.Title ?? "Unknown";

            var gameImagesDir = Path.Combine(imagesFolder, SanitizeFolderName(platformFolder), SanitizeFolderName(title));

            if (Directory.Exists(gameImagesDir))
            {
                Directory.Delete(gameImagesDir, true);
                RommLogger.Log($"Deleted images for removed game: {title}");
            }
        }

        private string SanitizeFolderName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return sanitized.Trim();
        }
    }
}