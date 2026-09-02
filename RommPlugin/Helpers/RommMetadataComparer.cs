using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Helpers
{
    /// <summary>
    /// Specifies the direction in which metadata should be synchronized.
    /// </summary>
    public enum SyncDirection
    {
        /// <summary>No synchronization is required.</summary>
        None,

        /// <summary>Metadata should be pushed from LaunchBox to RomM.</summary>
        PushToRomm,

        /// <summary>Metadata should be pulled from RomM to LaunchBox.</summary>
        PullFromRomm
    }

    /// <summary>
    /// Represents the result of comparing metadata between a LaunchBox game and a RomM game.
    /// </summary>
    public class MetadataComparisonResult
    {
        /// <summary>Gets or sets the determined sync direction.</summary>
        public SyncDirection Direction { get; set; }

        /// <summary>Gets or sets the list of field names that differ between local and remote metadata.</summary>
        public List<string> ChangedFields { get; set; } = new List<string>();

        /// <summary>Gets or sets a value indicating whether any metadata fields have changed.</summary>
        public bool MetadataChanged { get; set; }

        /// <summary>Gets or sets a value indicating whether artwork availability differs between local and remote.</summary>
        public bool ArtworkChanged { get; set; }

        /// <summary>Gets or sets a value indicating whether the screenshot count differs between local and remote.</summary>
        public bool ScreenshotsChanged { get; set; }

        /// <summary>Gets or sets a value indicating whether local artwork exists.</summary>
        public bool HasLocalArtwork { get; set; }

        /// <summary>Gets or sets a value indicating whether remote artwork exists.</summary>
        public bool HasRemoteArtwork { get; set; }
    }

    /// <summary>
    /// Compares metadata between LaunchBox games and RomM games to determine synchronization direction.
    /// </summary>
    public static class RommMetadataComparer
    {
        /// <summary>
        /// Compares local LaunchBox metadata with remote RomM metadata and determines the sync direction.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <param name="remote">The remote RomM game data.</param>
        /// <param name="lastSyncedAt">The timestamp of the last synchronization, or <c>null</c> if never synced.</param>
        /// <param name="localScreenshotCount">The number of local screenshots.</param>
        /// <param name="remoteScreenshotCount">The number of remote screenshots.</param>
        /// <param name="hasLocalArtwork">Whether local artwork exists.</param>
        /// <param name="hasRemoteArtwork">Whether remote artwork exists.</param>
        /// <returns>A <see cref="MetadataComparisonResult"/> describing the differences and recommended sync direction.</returns>
        public static MetadataComparisonResult Compare(
            IGame game,
            RommGame remote,
            DateTime? lastSyncedAt,
            int localScreenshotCount,
            int remoteScreenshotCount,
            bool hasLocalArtwork,
            bool hasRemoteArtwork)
        {
            var result = new MetadataComparisonResult();

            var lbSnapshot = BuildLaunchBoxSnapshot(game);
            var remoteSnapshot = BuildRommSnapshot(remote);

            CompareString(result, "Name", lbSnapshot.Name, remoteSnapshot.Name);
            CompareString(result, "Notes", lbSnapshot.Notes, remoteSnapshot.Notes);
            CompareLong(result, "ReleaseDate", lbSnapshot.ReleaseDate, remoteSnapshot.ReleaseDate);
            CompareInt(result, "MaxPlayers", lbSnapshot.MaxPlayers, remoteSnapshot.MaxPlayers);
            CompareString(result, "ReleaseType", lbSnapshot.ReleaseType, remoteSnapshot.ReleaseType);
            CompareString(result, "PlayMode", lbSnapshot.PlayMode, remoteSnapshot.PlayMode);
            CompareString(result, "VideoUrl", lbSnapshot.VideoUrl, remoteSnapshot.VideoUrl);
            CompareFloat(result, "CommunityRating", lbSnapshot.CommunityRating, remoteSnapshot.CommunityRating);
            CompareInt(result, "CommunityStarRatingTotalVotes", lbSnapshot.CommunityStarRatingTotalVotes, remoteSnapshot.CommunityStarRatingTotalVotes);
            CompareString(result, "WikipediaUrl", lbSnapshot.WikipediaUrl, remoteSnapshot.WikipediaUrl);
            CompareString(result, "Rating", lbSnapshot.Rating, remoteSnapshot.Rating);
            CompareStringList(result, "Genres", lbSnapshot.Genres, remoteSnapshot.Genres);
            CompareStringList(result, "Developers", lbSnapshot.Developers, remoteSnapshot.Developers);
            CompareStringList(result, "Publishers", lbSnapshot.Publishers, remoteSnapshot.Publishers);

            result.MetadataChanged = result.ChangedFields.Count > 0;
            result.ScreenshotsChanged = localScreenshotCount != remoteScreenshotCount;
            result.HasLocalArtwork = hasLocalArtwork;
            result.HasRemoteArtwork = hasRemoteArtwork;
            result.ArtworkChanged = hasLocalArtwork != hasRemoteArtwork;

            if (result.MetadataChanged)
            {
                if (remote.UpdatedAt != null && lastSyncedAt != null && remote.UpdatedAt > lastSyncedAt)
                {
                    result.Direction = SyncDirection.PullFromRomm;
                }
                else
                {
                    result.Direction = SyncDirection.PushToRomm;
                }
            }
            else if (result.ArtworkChanged)
            {
                if (hasRemoteArtwork && !hasLocalArtwork)
                {
                    result.Direction = SyncDirection.PullFromRomm;
                }
                else
                {
                    result.Direction = SyncDirection.PushToRomm;
                }
            }
            else if (result.ScreenshotsChanged)
            {
                result.Direction = SyncDirection.PushToRomm;
            }
            else
            {
                result.Direction = SyncDirection.None;
            }

            return result;
        }

        private static void CompareString(MetadataComparisonResult result, string fieldName, string local, string remote)
        {
            var l = local ?? "";
            var r = remote ?? "";
            if (!string.Equals(l, r, StringComparison.OrdinalIgnoreCase))
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareLong(MetadataComparisonResult result, string fieldName, long? local, long? remote)
        {
            if (local != remote)
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareInt(MetadataComparisonResult result, string fieldName, int? local, int? remote)
        {
            if (local != remote)
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareFloat(MetadataComparisonResult result, string fieldName, float local, float remote)
        {
            if (Math.Abs(local - remote) > 0.001f)
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareStringList(MetadataComparisonResult result, string fieldName, List<string> local, List<string> remote)
        {
            var l = local ?? new List<string>();
            var r = remote ?? new List<string>();
            if (l.Count != r.Count || !l.SequenceEqual(r))
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        /// <summary>
        /// Builds a <see cref="MetadataSnapshot"/> from a LaunchBox game's properties.
        /// </summary>
        /// <param name="game">The LaunchBox game to snapshot.</param>
        /// <returns>A <see cref="MetadataSnapshot"/> populated with the game's metadata.</returns>
        public static MetadataSnapshot BuildLaunchBoxSnapshot(IGame game)
        {
            var genres = new List<string>();
            if (!string.IsNullOrEmpty(game.GenresString))
            {
                genres = game.GenresString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim()).ToList();
            }

            var developers = new List<string>();
            if (!string.IsNullOrEmpty(game.Developer))
            {
                developers = game.Developer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim()).ToList();
            }

            var publishers = new List<string>();
            if (!string.IsNullOrEmpty(game.Publisher))
            {
                publishers = game.Publisher.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()).ToList();
            }

            return new MetadataSnapshot
            {
                Name = game.Title ?? "",
                Notes = StripHtmlTags(game.Notes ?? ""),
                ReleaseDate = game.ReleaseDate.HasValue
                    ? (long?)new DateTimeOffset(
                        DateTime.SpecifyKind(game.ReleaseDate.Value.Date, DateTimeKind.Utc)
                    ).ToUnixTimeSeconds()
                    : (long?)null,
                MaxPlayers = game.MaxPlayers,
                ReleaseType = game.ReleaseType ?? "",
                PlayMode = game.PlayMode ?? "",
                VideoUrl = game.VideoUrl ?? "",
                CommunityRating = game.CommunityStarRating,
                CommunityStarRatingTotalVotes = game.CommunityStarRatingTotalVotes > 0
                    ? (int?)game.CommunityStarRatingTotalVotes : null,
                WikipediaUrl = game.WikipediaUrl ?? "",
                Rating = game.Rating ?? "",
                Genres = genres,
                Developers = developers,
                Publishers = publishers
            };
        }

        /// <summary>
        /// Builds a <see cref="MetadataSnapshot"/> from a RomM game, merging data from multiple metadata sources.
        /// </summary>
        /// <param name="remote">The remote RomM game to snapshot.</param>
        /// <returns>A <see cref="MetadataSnapshot"/> populated with the merged remote metadata.</returns>
        public static MetadataSnapshot BuildRommSnapshot(RommGame remote)
        {
            var lb = remote.LaunchBoxMetadata;
            var ss = remote.SsMetadata;
            var igdb = remote.IgdbMetadata;
            var meta = remote.Metadatum;

            long? releaseDate = null;
            if (lb?.FirstReleaseDate != null) releaseDate = lb.FirstReleaseDate;
            else if (igdb?.FirstReleaseDate != null) releaseDate = igdb.FirstReleaseDate;
            else if (meta?.FirstReleaseDate != null) releaseDate = meta.FirstReleaseDate;

            int? maxPlayers = null;
            if (lb?.MaxPlayers != null) maxPlayers = lb.MaxPlayers;
            else if (ss?.Players != null && int.TryParse(ss.Players, out var p)) maxPlayers = p;

            var playMode = "";
            if (lb?.Cooperative == true) playMode = "Cooperative";

            var videoId = lb?.YoutubeVideoId ?? igdb?.YoutubeVideoId;
            var videoUrl = !string.IsNullOrEmpty(videoId) ? $"https://www.youtube.com/watch?v={videoId}" : "";

            float communityRating = 0;
            if (lb != null && lb.CommunityRating > 0) communityRating = lb.CommunityRating;
            else if (igdb != null && igdb.TotalRating.HasValue) communityRating = (float)igdb.TotalRating.Value;
            else if (meta != null && meta.AverageRating.HasValue) communityRating = (float)meta.AverageRating.Value;

            int? communityRatingCount = null;
            if (lb != null && lb.CommunityRatingCount > 0) communityRatingCount = lb.CommunityRatingCount;

            var releaseType = lb?.ReleaseType ?? "";
            var wikipediaUrl = lb?.WikipediaUrl ?? "";
            var esrb = lb?.Esrb ?? "";

            var genres = new List<string>();
            if (lb?.Genres != null && lb.Genres.Count > 0) genres = lb.Genres;
            else if (ss?.Genres != null && ss.Genres.Count > 0) genres = ss.Genres;
            else if (igdb?.Genres != null && igdb.Genres.Count > 0) genres = igdb.Genres;
            else if (meta?.Genres != null && meta.Genres.Count > 0) genres = meta.Genres;

            var developers = new List<string>();
            if (lb?.Companies != null && lb.Companies.Count > 0) developers = lb.Companies;
            else if (igdb?.Companies != null && igdb.Companies.Count > 0) developers = igdb.Companies;
            else if (meta?.Companies != null && meta.Companies.Count > 0) developers = meta.Companies;

            var publishers = new List<string>();
            if (ss?.Editeur != null && !string.IsNullOrEmpty(ss.Editeur)) publishers.Add(ss.Editeur);
            else if (ss?.Publisher != null && !string.IsNullOrEmpty(ss.Publisher)) publishers.Add(ss.Publisher);
            else if (developers.Count > 0) publishers.AddRange(developers);

            var summary = ss?.Synopsis ?? ss?.Description ?? remote.Summary ?? "";

            return new MetadataSnapshot
            {
                Name = remote.Name ?? "",
                Notes = summary,
                ReleaseDate = releaseDate,
                MaxPlayers = maxPlayers,
                ReleaseType = releaseType,
                PlayMode = playMode,
                VideoUrl = videoUrl,
                CommunityRating = communityRating,
                CommunityStarRatingTotalVotes = communityRatingCount,
                WikipediaUrl = wikipediaUrl,
                Rating = esrb,
                Genres = genres,
                Developers = developers,
                Publishers = publishers
            };
        }

        /// <summary>
        /// Computes a SHA-256 hash of the essential remote metadata fields for change detection.
        /// </summary>
        /// <param name="remote">The remote RomM game.</param>
        /// <returns>A Base64-encoded SHA-256 hash string.</returns>
        public static string ComputeRemoteMetadataHash(RommGame remote)
        {
            var pathCover = StripQueryString(remote.PathCoverSmall ?? "");
            var urlCover = StripQueryString(remote.UrlCover ?? "");

            var payload = string.Join("|",
                remote.Name ?? "",
                remote.Summary ?? "",
                pathCover,
                urlCover);

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static string StripQueryString(string url)
        {
            var idx = url.IndexOf('?');
            return idx >= 0 ? url.Substring(0, idx) : url;
        }

        private static string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            return Regex.Replace(html, "<[^>]+>", "").Trim();
        }

        /// <summary>
        /// Computes a SHA-256 hash of the local LaunchBox game metadata for change detection.
        /// </summary>
        /// <param name="game">The local LaunchBox game.</param>
        /// <param name="screenshotFingerprints">Optional screenshot fingerprint string to include in the hash payload.</param>
        /// <returns>A Base64-encoded SHA-256 hash string.</returns>
        public static string ComputeLocalMetadataHash(IGame game, string screenshotFingerprints = "")
        {
            var snapshot = BuildLaunchBoxSnapshot(game);

            var payload = string.Join("|",
                snapshot.Name ?? "",
                snapshot.Notes ?? "",
                snapshot.ReleaseDate?.ToString() ?? "",
                snapshot.MaxPlayers?.ToString() ?? "",
                snapshot.ReleaseType ?? "",
                snapshot.PlayMode ?? "",
                snapshot.VideoUrl ?? "",
                snapshot.CommunityRating.ToString("F2"),
                snapshot.CommunityStarRatingTotalVotes?.ToString() ?? "",
                snapshot.WikipediaUrl ?? "",
                snapshot.Rating ?? "",
                string.Join(",", snapshot.Genres ?? new List<string>()),
                string.Join(",", snapshot.Developers ?? new List<string>()),
                string.Join(",", snapshot.Publishers ?? new List<string>()),
                screenshotFingerprints ?? "");

            RommLogger.Log($"[HASH-DIAG] Game '{game.Title}' Platform='{game.Platform}'");
            RommLogger.Log($"[HASH-DIAG]   game.ReleaseDate={(game.ReleaseDate.HasValue ? game.ReleaseDate.Value.ToString("o") : "null")} Kind={game.ReleaseDate?.Kind}");
            RommLogger.Log($"[HASH-DIAG]   snapshot.ReleaseDate={snapshot.ReleaseDate}");
            RommLogger.Log($"[HASH-DIAG]   game.CommunityStarRating={game.CommunityStarRating} snapshot.F2={snapshot.CommunityRating.ToString("F2")}");
            RommLogger.Log($"[HASH-DIAG]   game.Notes={(game.Notes != null ? $"len={game.Notes.Length}" : "null")}");
            RommLogger.Log($"[HASH-DIAG]   game.GenresString='{game.GenresString}'");
            RommLogger.Log($"[HASH-DIAG]   game.Developer='{game.Developer}'");
            RommLogger.Log($"[HASH-DIAG]   game.Publisher='{game.Publisher}'");
            RommLogger.Log($"[HASH-DIAG]   game.ReleaseType='{game.ReleaseType}' game.PlayMode='{game.PlayMode}' game.VideoUrl='{game.VideoUrl}'");
            RommLogger.Log($"[HASH-DIAG]   game.WikipediaUrl='{game.WikipediaUrl}' game.Rating='{game.Rating}'");
            RommLogger.Log($"[HASH-DIAG]   payload_first100='{payload.Substring(0, Math.Min(100, payload.Length))}'");

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                var hash = sha256.ComputeHash(bytes);
                var result = Convert.ToBase64String(hash);
                RommLogger.Log($"[HASH-DIAG]   => hash={result}");
                return result;
            }
        }
    }

    /// <summary>
    /// Represents a snapshot of game metadata fields used for comparison between local and remote sources.
    /// </summary>
    public class MetadataSnapshot
    {
        /// <summary>Gets or sets the game title.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the game notes or description.</summary>
        public string Notes { get; set; }

        /// <summary>Gets or sets the release date as a Unix timestamp in seconds, or <c>null</c> if unknown.</summary>
        public long? ReleaseDate { get; set; }

        /// <summary>Gets or sets the maximum number of supported players, or <c>null</c> if unknown.</summary>
        public int? MaxPlayers { get; set; }

        /// <summary>Gets or sets the release type (e.g., "Full Game", "Demo").</summary>
        public string ReleaseType { get; set; }

        /// <summary>Gets or sets the play mode (e.g., "Cooperative").</summary>
        public string PlayMode { get; set; }

        /// <summary>Gets or sets the URL to a gameplay video.</summary>
        public string VideoUrl { get; set; }

        /// <summary>Gets or sets the community rating score.</summary>
        public float CommunityRating { get; set; }

        /// <summary>Gets or sets the total number of community votes, or <c>null</c> if unavailable.</summary>
        public int? CommunityStarRatingTotalVotes { get; set; }

        /// <summary>Gets or sets the Wikipedia page URL for the game.</summary>
        public string WikipediaUrl { get; set; }

        /// <summary>Gets or sets the content rating (e.g., ESRB rating).</summary>
        public string Rating { get; set; }

        /// <summary>Gets or sets the list of genre names.</summary>
        public List<string> Genres { get; set; }

        /// <summary>Gets or sets the list of developer names.</summary>
        public List<string> Developers { get; set; }

        /// <summary>Gets or sets the list of publisher names.</summary>
        public List<string> Publishers { get; set; }
    }
}
